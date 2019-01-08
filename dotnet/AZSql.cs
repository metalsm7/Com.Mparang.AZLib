using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
#if NET_STD || NET_CORE || NET_FX || NET_STORE
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using MongoDB;
using MongoDB.Driver;
using MongoDB.Bson;
#endif

namespace Com.Mparang.AZLib {
	/// <summary>RDBMS에 대한 Query Helper Class</summary>
	public class AZSql {
		/// <summary>PreparedStatement 또는 StoredProcedure에 사용하기 위한 전달값 Class</summary>
		public class ParameterData {
			public object DbType {get;set;}
			public int? Size {get;set;}
			public object Value {get;set;}
			/// <summary>기본 생성자</summary>
			public ParameterData() {}
			public ParameterData(object value) {
				this.Value = value;
			}
			/// <summary>생성자(for SqlServer), DbType, Value 지정</summary>
			public ParameterData(object value, SqlDbType dbType) {
				this.Value = value;
				this.DbType = dbType;
			}
			/// <summary>생성자(for SqlServer), DbType, Value, Size 지정</summary>
			public ParameterData(object value, SqlDbType dbType, int size) {
				this.Value = value;
				this.DbType = dbType;
				this.Size = size;
			}
#if NET_STD || NET_CORE || NET_FX || NET_STORE
			/// <summary>생성자(for PostgresQl), DbType, Value 지정</summary>
			public ParameterData(object value, NpgsqlTypes.NpgsqlDbType dbType) {
				this.Value = value;
				this.DbType = dbType;
			}
			/// <summary>생성자(for PostgresQl), DbType, Value, Size 지정</summary>
			public ParameterData(object value, NpgsqlTypes.NpgsqlDbType dbType, int size) {
				this.Value = value;
				this.DbType = dbType;
				this.Size = size;
			}
			/// <summary>생성자(for MySql), DbType, Value 지정</summary>
			public ParameterData(object value, MySqlDbType dbType) {
				this.Value = value;
				this.DbType = dbType;
			}
			/// <summary>생성자(for MySql), DbType, Value, Size 지정</summary>
			public ParameterData(object value, MySqlDbType dbType, int size) {
				this.Value = value;
				this.DbType = dbType;
				this.Size = size;
			}
#endif
			/// <summary>DbType 반환(for SqlServer)</summary>
			public SqlDbType GetSqlDbType() {
				return (SqlDbType)this.DbType;
			}
#if NET_STD || NET_CORE || NET_FX || NET_STORE
			/// <summary>DbType 반환(for PostgresQl)</summary>
			public NpgsqlTypes.NpgsqlDbType GetNpgsqlDbType() {
				return (NpgsqlTypes.NpgsqlDbType)this.DbType;
			}
			/// <summary>DbType 반환(for MySql)</summary>
			public MySqlDbType GetMySqlDbType() {
				return (MySqlDbType)this.DbType;
			}
#endif
		}
		public enum SQL_TYPE {
			MYSQL, SQLITE, SQLITE_ANDROID, MSSQL, MARIADB, ORACLE, POSTGRESQL
		}
		public const string SQL_TYPE_MYSQL = "mysql";											 // not using
		public const string SQL_TYPE_SQLITE = "sqlite";										 // Microsoft.Data.Sqlite
		public const string SQL_TYPE_SQLITE_ANDROID = "sqlite_android";		 // ?? sqldroid-1.0.3
		public const string SQL_TYPE_MSSQL = "mssql";											 // System.Data.SqlClient
		public const string SQL_TYPE_MARIADB = "mariadb";									 // not using
		public const string SQL_TYPE_ORACLE = "oracle";										 // not using
		public const string SQL_TYPE_POSTGRESQL = "postgresql";						 // Npgsql

		public static class ATTRIBUTE_COLUMN {
			public const string LABEL = "attribute_column_label";
			public const string NAME = "attribute_column_name";
			public const string TYPE = "attribute_column_type";
			public const string TYPE_NAME = "attribute_column_type_name";
			public const string SCHEMA_NAME = "attribute_column_schema_name";
			public const string DISPLAY_SIZE = "attribute_column_display_size";
			public const string SCALE = "attribute_column_scale";
			public const string PRECISION = "attribute_column_precision";
			public const string IS_AUTO_INCREMENT = "attribute_column_auto_increment";
			public const string IS_CASE_SENSITIVE = "attribute_column_case_sensitive";
			public const string IS_NULLABLE = "attribute_column_is_nullable";
			public const string IS_READONLY = "attribute_column_is_readonly";
			public const string IS_WRITABLE = "attribute_column_is_writable";
			public const string IS_SIGNED = "attribute_column_is_signed";
		}

		//
		private string query;
		private AZData parameters;
		private AZData return_parameters;
		private bool identity;

		// BeginTran을 통해 트랜잭션 처리 하는 도중 commit 오류 발생시 실행하기 위한 Action
		private Action<Exception> action_tran_on_commit;
		// BeginTran을 통해 트랜잭션 처리 하는 도중 commit 오류 발생시 실행하는 Rollback 처리 중 오류가 발생하는 경우 실행하기 위한 Action
		private Action<Exception> action_tran_on_rollback;
		// BeginTran 메소드를 통해 트랜잭션이 처리중인지를 확인하기 위한 변수
		private bool in_transaction = false;
		// 트랜잭션 처리 중 반환되는 값들을 저장하기 위한 데이터 자료
		private AZData transaction_result;
		// SP 처리 여부 확인용 변수
		private bool is_stored_procedure = false;
		private DBConnectionInfo db_info = null;
		private bool connected = false;
		private static AZSql this_object = null;
		//
		private SqlConnection sqlConnection = null;
#if NET_STD || NET_CORE || NET_FX || NET_STORE
		private NpgsqlConnection npgsqlConnection = null;
		private MySqlConnection mySqlConnection = null;
		private SqliteConnection sqliteConnection = null;
#endif
		//
		private SqlCommand sqlCommand = null;
#if NET_STD || NET_CORE || NET_FX || NET_STORE
		private NpgsqlCommand npgsqlCommand = null;
		private MySqlCommand mySqlCommand = null;
		private SqliteCommand sqliteCommand = null;
#endif
		// 트랜잭션 처리시 사용 변수
		private SqlTransaction sqlTransaction = null;
#if NET_STD || NET_CORE || NET_FX || NET_STORE
		private MySqlTransaction mySqlTransaction = null;
		private NpgsqlTransaction npgsqlTransaction = null;
#endif

		/// <summary>static Class 객체 반환 처리</summary>
		public static AZSql getInstance() {
			if (this_object == null) this_object = new AZSql ();
			return this_object;
		}

		/// <summary>기본 생성자</summary>
		public AZSql() {}

		/// <summary>생성자, 연결 문자열</summary>
		public AZSql(string json) {
			Set(json);
		}

		/// Created in 2015-08-19, leeyonghun
		public AZSql(DBConnectionInfo db_connection_info) {
			this.db_info = db_connection_info;
		}

		/// Created in 2015-08-19, leeyonghun
		public AZSql Set(string json) {
			this.db_info = new DBConnectionInfo(json);
			return this;
		}

		/// Created in 2015-08-19, leeyonghun
		public static AZSql Init(string json) {
			return new AZSql (json);
		}

		/// Created in 2015-08-19, leeyonghun
		public static AZSql Init(DBConnectionInfo db_connection_info) {
			return new AZSql(db_connection_info);
		}

		public AZSql Clear() {
			SetIsStoredProcedure(false);
			ClearQuery();
			ClearParameters();
			ClearReturnParameters();
			return this;
		}
		
		/// <summary></summary>
		/// Created in 2017-03-28, leeyonghun
		public Prepared GetPrepared() {
			return new Prepared(this);
		}

		/// <summary>현재 연결객체의 SqlType값을 반환</summary>
		/// Created in 2017-06-27, leeyonghun
		public SQL_TYPE GetSqlType() {
			return this.db_info.SqlType;
		}

		/// <summary></summary>
		/// <param name="on_commit">Action<Exception>, commit 처리 및 이전 쿼리 처리 진행 중 예외가 발생하는 경우 처리를 하기 위한 Action</param>
		/// <param name="on_rollback">Action<Exception>, commit 처리중 예외 발생으로 rollback처리 중 예외가 발생하는 경우 처리를 위한 Action</param>
		/// <example>
		/// <code>
		/// 트랜잭션 처리 시작을 알리며, 이후 Commit때까지 트랜잭션 진행, 
		/// 사용예)
		/// AZSql sql = new AZSql("~~~");
		/// sql.BeginTran(
		///		 (ex_commit) => Console.WriteLine("on_commit : " + ex_commit.ToString()), 
		///		 (ex_commit) => Console.WriteLine("on_commit : " + ex_commit.ToString()));
		/// 
		/// sql.Execute("SELECT 1;");
		/// sql.Get("SELECT 2;");
		/// sql.GetData("SELECT 'v1' as k1, 'v2' as k2, 'v3' as k3;");
		///
		/// AZSql.Basic basic = new AZSql.Basic("IntServiceTbl", sql, true);
		/// basic.Set("k1", "v1");
		/// basic.Where("idx", 1);
		/// basic.DoUpdate();
		///
		/// AZData result = sql.Commit();
		/// </code>
		/// </example>
		/// Created in 2017-06-27, leeyonghun
		public void BeginTran(Action<Exception> on_commit, Action<Exception> on_rollback) {
			if (sqlConnection == null) Open();
			//
			switch (this.GetSqlType()) {
				case AZSql.SQL_TYPE.MSSQL:
					sqlTransaction = sqlConnection.BeginTransaction();
					break;
				case AZSql.SQL_TYPE.MYSQL:
					mySqlTransaction = mySqlConnection.BeginTransaction();
					break;
				case AZSql.SQL_TYPE.POSTGRESQL:
					npgsqlTransaction = npgsqlConnection.BeginTransaction();
					break;
			}
			transaction_result = new AZData();
			//
			this.action_tran_on_commit = on_commit;
			this.action_tran_on_rollback = on_rollback;
			//
			this.in_transaction = true;
		}

		/// <summary>현재 AZSql객체에서 트랜잭션 진행 정보를 삭제 및 초기화</summary>
		public void RemoveTran() {
			sqlTransaction = null;
			mySqlTransaction = null;
			//
			transaction_result = null;
			//
			this.in_transaction = false;
			//
			//transaction_result = null;
			//
			Close();
		}
		
		/// <summary>현재 AZSql객체에서 트랜잭션에 대한 Callback 초기화 처리</summary>
		public void ClearTransCallback() {
			this.action_tran_on_commit = null;
			this.action_tran_on_rollback = null;
		}

		/// <summary>트랜잭션 commit 처리</summary>
		/// <return>AZData, 트랜잭션 처리 중 발생한 반환값들의 집합인 AZData를 반환</return>
		public AZData Commit() {
			AZData rtn_value = null;
			Exception exception_commit = null;
			Exception exception_rollback = null;
			try {
				if (connected) {
					switch (this.GetSqlType()) {
						case AZSql.SQL_TYPE.MSSQL: sqlTransaction.Commit(); break;
						case AZSql.SQL_TYPE.MYSQL: mySqlTransaction.Commit(); break;
						case AZSql.SQL_TYPE.POSTGRESQL: npgsqlTransaction.Commit(); break;
					}
					rtn_value = transaction_result;
				}
			}
			catch (Exception ex) {
				exception_commit = ex;
				//
				try {
					switch (this.GetSqlType()) {
						case AZSql.SQL_TYPE.MSSQL: sqlTransaction.Rollback(); break;
						case AZSql.SQL_TYPE.MYSQL: mySqlTransaction.Rollback(); break;
						case AZSql.SQL_TYPE.POSTGRESQL: npgsqlTransaction.Rollback(); break;
					}
				}
				catch (Exception ex_rollback) {
					exception_rollback = ex_rollback;
				}
			}
			finally {
				RemoveTran();
				//
				if (exception_commit != null && this.action_tran_on_commit != null) {
					this.action_tran_on_commit(exception_commit);
				}
				if (exception_rollback != null && this.action_tran_on_rollback != null) {
					this.action_tran_on_rollback(exception_rollback);
				}
				//
				ClearTransCallback();
			}
			return rtn_value;
		}

		/// <summary>트랜잭션 tollback 처리</summary>
		/// <return>AZData, 트랜잭션 처리 중 발생한 반환값들의 집합인 AZData를 반환</return>
		public AZData Rollback() {
			AZData rtn_value = null;
			
			if (!this.in_transaction) {
				throw new Exception("Not in transaction process.1");
			}
			if (this.GetSqlType() == AZSql.SQL_TYPE.MSSQL && sqlTransaction == null ||
				this.GetSqlType() == AZSql.SQL_TYPE.MYSQL && mySqlTransaction == null ||
				this.GetSqlType() == AZSql.SQL_TYPE.POSTGRESQL && npgsqlTransaction == null) {
				throw new Exception("Not in transaction process.2");
			}
			Exception exception_rollback = null;
			try {
				if (connected) {
					switch (this.GetSqlType()) {
						case AZSql.SQL_TYPE.MSSQL: sqlTransaction.Rollback(); break;
						case AZSql.SQL_TYPE.MYSQL: mySqlTransaction.Rollback(); break;
						case AZSql.SQL_TYPE.POSTGRESQL: npgsqlTransaction.Rollback(); break;
					}
				}
			}
			catch (Exception ex) {
				exception_rollback = ex;
			}
			finally {
				RemoveTran();
				//
				if (exception_rollback != null && this.action_tran_on_rollback != null) {
					this.action_tran_on_rollback(exception_rollback);
				}
				//
				ClearTransCallback();
			}
			return rtn_value;
		}

		/// <summary>현재 AZSql 객체에 대해 실행할 쿼리문 설정</summary>
		/// <param name="query">string, 쿼리문</param>
		public AZSql SetQuery(string query) {
			this.query = query;
			return this;
		}

		/// <summary>현재 AZSql 객체에 대해 설정된 쿼리문 반환</summary>
		public string GetQuery() {
			return this.query;
		}

		public AZSql ClearQuery() {
			this.query = "";
			return this;
		}

		/// <summary>PreparedStatement 또는 StoredProcedure 사용의 경우 전달할 인수값 설정.
		/// 실제 처리는 AddParameter(string, object)의 반복</summary>
		public AZSql SetParameters(AZData parameters) {
			if (this.parameters == null) {
				this.parameters = new AZData();
			}
			else {
				this.parameters.Clear();
			}
			for (int cnti=0; cnti<parameters.Size(); cnti++) {
				this.parameters.Add(parameters.GetKey(cnti), new ParameterData(parameters.Get(cnti)));
			}
			return this;
		}

		/// <summary>현재 설정된 parameter 값 반환
		/// AZData(string key, ParameterData value) 형식으로 구성되어 있어, 
		/// value 값 사용시 캐스팅 필요</summary>
		public AZData GetParameters() {
			return this.parameters;
		}
		/// <summary>설정된 key에 해당하는 ParameterData 객체 반환</summary>
		/// <param name="key">string, Parameter 전달 key</param>
		public ParameterData GetParameter(string key) {
			return (ParameterData)this.parameters.Get(key);
		}
		/// <summary>설정된 key에 해당하는 ParameterData.Value 값 반환</summary>
		/// <param name="key">string, Parameter 전달 key</param>
		public object GetParameterValue(string key) {
			return GetParameter(key).Value;
		}
		/// <summary>설정된 key에 해당하는 ParameterData.Value 값에 대해 T형식으로 캐스팅하여 반환</summary>
		/// <param name="key">string, Parameter 전달 key</param>
		public T GetParameterValue<T>(string key) {
			return (T)GetParameter(key).Value;
		}
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		public AZSql AddParameter(string key, object value) {
			if (this.parameters == null) this.parameters = new AZData();
			this.parameters.Add(key, new ParameterData(value));
			return this;
		}
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">SqlDbType, value값의 DB TYPE 설정</param>
		public AZSql AddParameter(string key, object value, SqlDbType dbType) {
			if (this.parameters == null) this.parameters = new AZData();
			this.parameters.Add(key, new ParameterData(value, dbType));
			return this;
		}
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">SqlDbType, value값의 DB TYPE 설정</param>
		/// <param name="size">int, value값의 크기 설정</param>
		public AZSql AddParameter(string key, object value, SqlDbType dbType, int size) {
			if (this.parameters == null) this.parameters = new AZData();
			this.parameters.Add(key, new ParameterData(value, dbType, size));
			return this;
		}
#if NET_STD || NET_CORE || NET_FX || NET_STORE
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">NpgsqlTypes.NpgsqlDbType, value값의 DB TYPE 설정</param>
		public AZSql AddParameter(string key, object value, NpgsqlTypes.NpgsqlDbType dbType) {
			if (this.parameters == null) this.parameters = new AZData();
			this.parameters.Add(key, new ParameterData(value, dbType));
			return this;
		}
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">NpgsqlTypes.NpgsqlDbType, value값의 DB TYPE 설정</param>
		/// <param name="size">int, value값의 크기 설정</param>
		public AZSql AddParameter(string key, object value, NpgsqlTypes.NpgsqlDbType dbType, int size) {
			if (this.parameters == null) this.parameters = new AZData();
			this.parameters.Add(key, new ParameterData(value, dbType, size));
			return this;
		}
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">MySqlDbType, value값의 DB TYPE 설정</param>
		public AZSql AddParameter(string key, object value, MySqlDbType dbType) {
			if (this.parameters == null) this.parameters = new AZData();
			this.parameters.Add(key, new ParameterData(value, dbType));
			return this;
		}
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">MySqlDbType, value값의 DB TYPE 설정</param>
		/// <param name="size">int, value값의 크기 설정</param>
		public AZSql AddParameter(string key, object value, MySqlDbType dbType, int size) {
			if (this.parameters == null) this.parameters = new AZData();
			this.parameters.Add(key, new ParameterData(value, dbType, size));
			return this;
		}
#endif
		/// <summary>parameter 추가, AddParameters("key1", value1, "key2", value2...) 형식으로 사용</summary>
		/// <param name="parameters">키, 값 순서로 만들어진 object배열값</param>
		public AZSql AddParameters(params object[] parameters) {
			if (this.parameters == null) this.parameters = new AZData();
			for (int cnti=0; cnti<parameters.Length; cnti+=2) {
				this.parameters.Add(parameters[cnti].To<string>(), new ParameterData(parameters[cnti + 1]));
			}
			return this;
		}
		/// <summary>현재 AZSql객체에 설정된 parameter 값 초기화</summary>
		public void ClearParameters() {
			this.parameters.Clear();
		}
		/// <summary>현재 AZSql객체에 설정된 parameter 초기화 처리, 재사용시 새로운 객체 생성 절차가 포함됨</summary>
		public void RemoveParameters() {
			this.parameters = null;
		}

		/// <summary>PreparedStatement 또는 StoredProcedure 사용의 경우 전달할 리턴 인수값 설정.
		/// 실제 처리는 AddParameter(string, object)의 반복</summary>
		public AZSql SetReturnParameters(AZData parameters) {
			if (this.return_parameters == null) {
				this.return_parameters = new AZData();
			}
			else {
				this.return_parameters.Clear();				
			}
			for (int cnti=0; cnti<parameters.Size(); cnti++) {
				this.return_parameters.Add(parameters.GetKey(cnti), new ParameterData(parameters.Get(cnti)));
			}
			return this;
		}
		/// <summary>현재 설정된 return parameter 값 반환
		/// AZData(string key, ParameterData value) 형식으로 구성되어 있어, 
		/// value 값 사용시 캐스팅 필요</summary>
		public AZData GetReturnParameters() {
			return this.return_parameters;
		}
		/// <summary>설정된 key에 해당하는 ParameterData 객체 반환</summary>
		/// <param name="key">string, Parameter 전달 key</param>
		public ParameterData GetReturnParameter(string key) {
			return (ParameterData)this.return_parameters.Get(key);
		}
		/// <summary>설정된 key에 해당하는 ParameterData.Value 값 반환</summary>
		/// <param name="key">string, Parameter 전달 key</param>
		public object GetReturnParameterValue(string key) {
			return GetReturnParameter(key).Value;
		}
		/// <summary>설정된 key에 해당하는 ParameterData.Value 값에 대해 T형식으로 캐스팅하여 반환</summary>
		/// <param name="key">string, Parameter 전달 key</param>
		public T GetReturnParameterValue<T>(string key) {
			return (T)GetReturnParameter(key).Value;
		}
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		public AZSql AddReturnParameter(string key, object value) {
			if (this.return_parameters == null) this.return_parameters = new AZData();
			this.return_parameters.Add(key, new ParameterData(value));
			return this;
		}
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">SqlDbType, value값의 DB TYPE 설정</param>
		public AZSql AddReturnParameter(string key, object value, SqlDbType dbType) {
			if (this.return_parameters == null) this.return_parameters = new AZData();
			this.return_parameters.Add(key, new ParameterData(value, dbType));
			return this;
		}
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">SqlDbType, value값의 DB TYPE 설정</param>
		/// <param name="size">int, value값의 크기 설정</param>
		public AZSql AddReturnParameter(string key, object value, SqlDbType dbType, int size) {
			if (this.return_parameters == null) this.return_parameters = new AZData();
			this.return_parameters.Add(key, new ParameterData(value, dbType, size));
			return this;
		}
#if NET_STD || NET_CORE || NET_FX || NET_STORE
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">NpgsqlTypes.NpgsqlDbType, value값의 DB TYPE 설정</param>
		public AZSql AddReturnParameter(string key, object value, NpgsqlTypes.NpgsqlDbType dbType) {
			if (this.return_parameters == null) this.return_parameters = new AZData();
			this.return_parameters.Add(key, new ParameterData(value, dbType));
			return this;
		}
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">NpgsqlTypes.NpgsqlDbType, value값의 DB TYPE 설정</param>
		/// <param name="size">int, value값의 크기 설정</param>
		public AZSql AddReturnParameter(string key, object value, NpgsqlTypes.NpgsqlDbType dbType, int size) {
			if (this.return_parameters == null) this.return_parameters = new AZData();
			this.return_parameters.Add(key, new ParameterData(value, dbType, size));
			return this;
		}
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">MySqlDbType, value값의 DB TYPE 설정</param>
		public AZSql AddReturnParameter(string key, object value, MySqlDbType dbType) {
			if (this.return_parameters == null) this.return_parameters = new AZData();
			this.return_parameters.Add(key, new ParameterData(value, dbType));
			return this;
		}
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">MySqlDbType, value값의 DB TYPE 설정</param>
		/// <param name="size">int, value값의 크기 설정</param>
		public AZSql AddReturnParameter(string key, object value, MySqlDbType dbType, int size) {
			if (this.return_parameters == null) this.return_parameters = new AZData();
			this.return_parameters.Add(key, new ParameterData(value, dbType, size));
			return this;
		}
#endif
		/// <summary>return parameter 추가, AddReturnParameters("key1", value1, "key2", value2...) 형식으로 사용</summary>
		/// <param name="parameters">키, 값 순서로 만들어진 object배열값</param>
		public AZSql AddReturnParameters(params object[] parameters) {
			if (this.return_parameters == null) this.return_parameters = new AZData();
			for (int cnti=0; cnti<parameters.Length; cnti+=2) {
				this.return_parameters.Add(parameters[cnti].To<string>(), new ParameterData(parameters[cnti + 1]));
			}
			return this;
		}
		/// <summary>return parameter 에서 idx에 해당하는 자료의 값을 설정</summary>
		/// <param name="idx">return parameter에서의 index값</param>
		/// <param name="value">해당하는 ParameterData.Value에 설정할 값</param>
		public AZSql UpdateReturnParameter(int idx, object value) {
			ParameterData data = (ParameterData)this.return_parameters.Get(idx);
			data.Value = value;
			this.return_parameters.Set(idx, data);
			return this;
		}
		/// <summary>return parameter 에서 지정된 key값에 해당하는 자료의 값을 설정</summary>
		/// <param name="key">return parameter에서의 key값</param>
		/// <param name="value">해당하는 ParameterData.Value에 설정할 값</param>
		public AZSql UpdateReturnParameter(string key, object value) {
			ParameterData data = (ParameterData)this.return_parameters.Get(key);
			data.Value = value;
			this.return_parameters.Set(key, data);
			return this;
		}
		/// <summary>현재 AZSql객체에 설정된 return_parameter 값 초기화</summary>
		public void ClearReturnParameters() {
			this.return_parameters.Clear();
		}
		/// <summary>현재 AZSql객체에 설정된 return_parameter 초기화 처리, 재사용시 새로운 객체 생성 절차가 포함됨</summary>
		public void RemoveReturnParameters() {
			this.return_parameters = null;
		}

		/// <summary>insert 쿼리 실행 후 발생된 identity값을 반환 받을지 여부 설정</summary>
		public AZSql SetIdentity(bool identity) {
			this.identity = identity;
			return this;
		}

		/// <summary>insert 쿼리 실행 후 발생된 identity값을 반환 받을지 여부 반환</summary>
		public bool GetIdentity() {
			return this.identity;
		}

		/// <summary>실행할 쿼리가 stored procedure인지 여부 설정</summary>
		public AZSql SetIsStoredProcedure(bool is_stored_procedure) {
			this.is_stored_procedure = is_stored_procedure;
			return this;
		}

		/// <summary>실행할 쿼리가 stored procedure인지 여부 설정값 반환</summary>
		public bool IsStoredProcedure() {
			return this.is_stored_procedure;
		}

		/// <summary>지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public int Execute(string query) {
			SetQuery(query);
			return Execute();
		}
		
		/// <summary>지정된 쿼리문 실행 처리</summary>
		/// <param name="identity">쿼리 실행 후 발생되는 identity값 반환 여부 설정</param>
		public int Execute(bool identity) {
			SetIdentity(identity);
			return Execute();
		}
		
		/// <summary>지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="identity">쿼리 실행 후 발생되는 identity값 반환 여부 설정</param>
		public int Execute(string query, bool identity) {
			SetQuery(query);
			SetIdentity(identity);
			return Execute();
		}
				
		/// <summary>지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public int Execute(string query, AZData parameters) {
			SetQuery(query);
			SetParameters(parameters);
			return Execute();
		}
		
		/// <summary>지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public int Execute(string query, AZData parameters, bool identity) {
			SetQuery(query);
			SetParameters(parameters);
			SetIdentity(identity);
			return Execute();
		}
		
		/// <summary>지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters"></param>
		public int Execute(string query, AZData parameters, AZData returnParameters) {
			SetQuery(query);
			SetParameters(parameters);
			SetReturnParameters(returnParameters);
			return Execute();
		}
		
		/// <summary>지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters"></param>
		public int Execute(string query, AZData parameters, AZData returnParameters, bool isStoredProcedure) {
			SetIsStoredProcedure(isStoredProcedure);
			SetQuery(query);
			SetParameters(parameters);
			SetReturnParameters(returnParameters);
			return Execute();
		}
				
		/// <summary>지정된 쿼리문 실행 처리</summary>
		public int Execute() {
			int rtnValue = 0;
			if (in_transaction && !connected) return rtnValue;
			try {
				if (!connected) Open();
				if (connected) {
					switch (this.db_info.SqlType) {
						case SQL_TYPE.MSSQL:		// mssql 접속 처리시
							//sqlCommand = new SqlCommand(p_query, sqlConnection);
							sqlCommand = sqlConnection.CreateCommand();
							sqlCommand.CommandText = GetQuery();
							if (sqlTransaction != null) sqlCommand.Transaction = sqlTransaction;
							if (IsStoredProcedure()) sqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									//SqlParameter param = sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null);
									//param.Direction = ParameterDirection.Output;
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetIdentity()) {
								sqlCommand.ExecuteNonQuery();

								sqlCommand = new SqlCommand("SELECT @@IDENTITY;", sqlConnection);
								rtnValue = AZString.Init(sqlCommand.ExecuteScalar()).ToInt(-1);
							}
							else {
								rtnValue = sqlCommand.ExecuteNonQuery();
							}
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									//GetReturnParameters().Set(key, sqlCommand.Parameters[key].Value);
									UpdateReturnParameter(key, sqlCommand.Parameters[key].Value);
								}
							}
							break;
#if NET_STD || NET_CORE || NET_FX || NET_STORE
						case SQL_TYPE.SQLITE:
							//sqliteCommand = new SQLiteCommand(p_query, sqliteConnection);
							sqliteCommand = sqliteConnection.CreateCommand();
							sqliteCommand.CommandText = GetQuery();
							if (GetIdentity()) {
								sqliteCommand.ExecuteNonQuery();

								sqliteCommand = sqliteConnection.CreateCommand();
								sqliteCommand.CommandText = "SELECT last_insert_rowid();";
								rtnValue = AZString.Init(sqliteCommand.ExecuteScalar()).ToInt(-1);
							}
							else {
								rtnValue = sqliteCommand.ExecuteNonQuery();
							}
							break;
						case SQL_TYPE.POSTGRESQL:		// postgresql 접속 처리시
							npgsqlCommand = new NpgsqlCommand(GetQuery(), npgsqlConnection);
							if (npgsqlTransaction != null) npgsqlCommand.Transaction = npgsqlTransaction;
							if (IsStoredProcedure()) npgsqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							/*if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									npgsqlCommand.Parameters.AddWithValue(GetParameters().GetKey(cnti), GetParameters().Get(cnti));
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									npgsqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null).Direction = ParameterDirection.Output;
								}
							}*/
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									//SqlParameter param = sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null);
									//param.Direction = ParameterDirection.Output;
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetIdentity()) {
								npgsqlCommand.ExecuteNonQuery();

								npgsqlCommand = new NpgsqlCommand("SELECT @@IDENTITY;", npgsqlConnection);
								rtnValue = AZString.Init(npgsqlCommand.ExecuteScalar()).ToInt(-1);
							}
							else {
								rtnValue = npgsqlCommand.ExecuteNonQuery();
							}

							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									//GetReturnParameters().Set(key, npgsqlCommand.Parameters[key].Value);
									UpdateReturnParameter(key, npgsqlCommand.Parameters[key].Value);
								}
							}
							break;
						case SQL_TYPE.MYSQL:
							mySqlCommand = new MySqlCommand (GetQuery(), mySqlConnection);
							if (mySqlTransaction != null) mySqlCommand.Transaction = mySqlTransaction;
							if (IsStoredProcedure()) mySqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							/*if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									mySqlCommand.Parameters.AddWithValue(GetParameters().GetKey(cnti), GetParameters().Get(cnti));
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									mySqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null).Direction = ParameterDirection.Output;
								}
							}*/
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									//SqlParameter param = sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null);
									//param.Direction = ParameterDirection.Output;
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetIdentity()) {
								mySqlCommand.ExecuteNonQuery();

								mySqlCommand = new MySqlCommand("SELECT LAST_INSERT_ID();", mySqlConnection);
								rtnValue = AZString.Init(mySqlCommand.ExecuteScalar()).ToInt(-1);
							}
							else {
								rtnValue = mySqlCommand.ExecuteNonQuery();
							}

							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									//GetReturnParameters().Set(key, mySqlCommand.Parameters[key].Value);
									UpdateReturnParameter(key, mySqlCommand.Parameters[key].Value);
								}
							}
							break;
#endif
					}
				}
			}
			catch (Exception ex) {
				if (sqlTransaction == null) {
					if (ex.InnerException != null) {
						throw new Exception("Exception in Execute.Inner", ex.InnerException);
					}
					else {
						throw new Exception("Exception in Execute", ex);
					}
				}
				else {
					Exception exception_rollback = null;
					try {
						switch (this.GetSqlType()) {
							case AZSql.SQL_TYPE.MSSQL: sqlTransaction.Rollback(); break;
							case AZSql.SQL_TYPE.MYSQL: mySqlTransaction.Rollback(); break;
							case AZSql.SQL_TYPE.POSTGRESQL: npgsqlTransaction.Rollback(); break;
						}
					}
					catch (Exception ex_rollback) {
						exception_rollback = ex_rollback;
					}
					finally {
						RemoveTran();
						//
						if (this.action_tran_on_commit != null) {
							this.action_tran_on_commit(ex);
						}
						if (exception_rollback != null && this.action_tran_on_rollback != null) {
							this.action_tran_on_rollback(exception_rollback);
						}
						//
						ClearTransCallback();
					}
				}
			}
			finally {
				//if (sqlTransaction == null) Close ();
				switch (this.GetSqlType()) {
					case AZSql.SQL_TYPE.MSSQL: if (sqlTransaction == null) Close (); break;
					case AZSql.SQL_TYPE.MYSQL: if (mySqlTransaction == null) Close (); break;
					case AZSql.SQL_TYPE.POSTGRESQL: if (npgsqlTransaction == null) Close (); break;
				}
			}

			if ((sqlTransaction != null || mySqlTransaction != null || npgsqlTransaction != null) && transaction_result != null) {
				transaction_result.Add("Execute." + (transaction_result.Size() + 1), rtnValue);
			}
			return rtnValue;
		}
		
#if NET_STD || NET_CORE || NET_FX || NET_STORE
		/// <summary>지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public async Task<int> ExecuteAsync(string query) {
			SetQuery(query);
			return await ExecuteAsync();
		}

		/// <summary>지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="identity">쿼리 실행 후 발생되는 identity값 반환 여부 설정</param>
		public async Task<int> ExecuteAsync(bool identity) {
			SetIdentity(identity);
			return await ExecuteAsync();
		}

		/// <summary>지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="identity">쿼리 실행 후 발생되는 identity값 반환 여부 설정</param>
		public async Task<int> ExecuteAsync(string query, bool identity) {
			SetQuery(query);
			SetIdentity(identity);
			return await ExecuteAsync();
		}

		/// <summary>지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		public async Task<int> ExecuteAsync(string query, AZData parameters) {
			SetQuery(query);
			SetParameters(parameters);
			return await ExecuteAsync();
		}

		/// <summary>지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public async Task<int> ExecuteAsync(string query, AZData parameters, bool identity) {
			SetQuery(query);
			SetParameters(parameters);
			SetIdentity(identity);
			return await ExecuteAsync();
		}
		
		/// <summary>지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters"></param>
		public async Task<int> ExecuteAsync(string query, AZData parameters, AZData returnParameters) {
			SetQuery(query);
			SetParameters(parameters);
			SetReturnParameters(returnParameters);
			return await ExecuteAsync();
		}
		
		/// <summary>지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters"></param>
		public async Task<int> ExecuteAsync(string query, AZData parameters, AZData returnParameters, bool isStoredProcedure) {
			SetIsStoredProcedure(isStoredProcedure);
			SetQuery(query);
			SetParameters(parameters);
			SetReturnParameters(returnParameters);
			return await ExecuteAsync();
		}

		/// <summary>지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<int> ExecuteAsync() {
			int rtnValue = 0;
			if (in_transaction && !connected) return rtnValue;
			try {
				if (!connected) await OpenAsync();
				if (connected) {
					switch (this.db_info.SqlType) {
						case SQL_TYPE.MSSQL:		// mssql 접속 처리시
							sqlCommand = sqlConnection.CreateCommand();
							sqlCommand.CommandText = GetQuery();
							if (sqlTransaction != null) sqlCommand.Transaction = sqlTransaction;
							if (IsStoredProcedure()) sqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetIdentity()) {
								await sqlCommand.ExecuteNonQueryAsync();
								//
								sqlCommand = new SqlCommand("SELECT @@IDENTITY;", sqlConnection);
								rtnValue = AZString.Init(await sqlCommand.ExecuteScalarAsync()).ToInt(-1);
							}
							else {
								rtnValue = await sqlCommand.ExecuteNonQueryAsync();
							}
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									UpdateReturnParameter(key, sqlCommand.Parameters[key].Value);
								}
							}
							break;
						case SQL_TYPE.SQLITE:
							sqliteCommand = sqliteConnection.CreateCommand();
							sqliteCommand.CommandText = GetQuery();
							if (GetIdentity()) {
								await sqliteCommand.ExecuteNonQueryAsync();
								//
								sqliteCommand = sqliteConnection.CreateCommand();
								sqliteCommand.CommandText = "SELECT last_insert_rowid();";
								rtnValue = AZString.Init(await sqliteCommand.ExecuteScalarAsync()).ToInt(-1);
							}
							else {
								rtnValue = await sqliteCommand.ExecuteNonQueryAsync();
							}
							break;
						case SQL_TYPE.POSTGRESQL:		// postgresql 접속 처리시
							//npgsqlCommand = new NpgsqlCommand(GetQuery(), npgsqlConnection);
							npgsqlCommand = npgsqlConnection.CreateCommand();
							npgsqlCommand.CommandText = GetQuery();
							if (npgsqlTransaction != null) npgsqlCommand.Transaction = npgsqlTransaction;
							if (IsStoredProcedure()) npgsqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetIdentity()) {
								await npgsqlCommand.ExecuteNonQueryAsync();
								//
								npgsqlCommand = new NpgsqlCommand("SELECT @@IDENTITY;", npgsqlConnection);
								rtnValue = AZString.Init(await npgsqlCommand.ExecuteScalarAsync()).ToInt(-1);
							}
							else {
								rtnValue = await npgsqlCommand.ExecuteNonQueryAsync();
							}
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									UpdateReturnParameter(key, npgsqlCommand.Parameters[key].Value);
								}
							}
							break;
						case SQL_TYPE.MYSQL:
							//mySqlCommand = new MySqlCommand (GetQuery(), mySqlConnection);
							mySqlCommand = mySqlConnection.CreateCommand();
							mySqlCommand.CommandText = GetQuery();
							if (mySqlTransaction != null) mySqlCommand.Transaction = mySqlTransaction;
							if (IsStoredProcedure()) mySqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetIdentity()) {
								await mySqlCommand.ExecuteNonQueryAsync();
								//
								mySqlCommand = new MySqlCommand("SELECT LAST_INSERT_ID();", mySqlConnection);
								rtnValue = AZString.Init(await mySqlCommand.ExecuteScalarAsync()).ToInt(-1);
							}
							else {
								rtnValue = await mySqlCommand.ExecuteNonQueryAsync();
							}
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									UpdateReturnParameter(key, mySqlCommand.Parameters[key].Value);
								}
							}
							break;
					}
				}
			}
			catch (Exception ex) {
				if (sqlTransaction == null) {
					if (ex.InnerException != null) {
						throw new Exception("Exception in Execute.Inner", ex.InnerException);
					}
					else {
						throw new Exception("Exception in Execute", ex);
					}
				}
				else {
					Exception exception_rollback = null;
					try {
						switch (this.GetSqlType()) {
							case AZSql.SQL_TYPE.MSSQL: sqlTransaction.Rollback(); break;
							case AZSql.SQL_TYPE.MYSQL: mySqlTransaction.Rollback(); break;
							case AZSql.SQL_TYPE.POSTGRESQL: npgsqlTransaction.Rollback(); break;
						}
					}
					catch (Exception ex_rollback) {
						exception_rollback = ex_rollback;
					}
					finally {
						RemoveTran();
						//
						if (this.action_tran_on_commit != null) {
							this.action_tran_on_commit(ex);
						}
						if (exception_rollback != null && this.action_tran_on_rollback != null) {
							this.action_tran_on_rollback(exception_rollback);
						}
						//
						ClearTransCallback();
					}
				}
			}
			finally {
				switch (this.GetSqlType()) {
					case AZSql.SQL_TYPE.MSSQL: if (sqlTransaction == null) Close (); break;
					case AZSql.SQL_TYPE.MYSQL: if (mySqlTransaction == null) Close (); break;
					case AZSql.SQL_TYPE.POSTGRESQL: if (npgsqlTransaction == null) Close (); break;
				}
			}

			if ((sqlTransaction != null || mySqlTransaction != null || npgsqlTransaction != null) && transaction_result != null) {
				transaction_result.Add("Execute." + (transaction_result.Size() + 1), rtnValue);
			}
			return rtnValue;
		}
#endif

		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public object Get(string query) {
			SetQuery(query);
			return Get();
		}
				
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="identity">쿼리 실행 후 발생되는 identity값 반환 여부 설정</param>
		public object Get(string query, AZData parameters) {
			SetQuery(query);
			SetParameters(parameters);
			return Get();
		}

		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		public object Get() {
			object rtnValue = null;
			//
			if (in_transaction && !connected) return rtnValue;
			try {
				if (!connected) Open();
				if (connected) {
					switch (this.db_info.SqlType) {
						case SQL_TYPE.MSSQL:		// mssql 접속 처리시
							//sqlCommand = new SqlCommand(p_query, sqlConnection);
							sqlCommand = sqlConnection.CreateCommand();
							sqlCommand.CommandText = GetQuery();
							if (sqlTransaction != null) sqlCommand.Transaction = sqlTransaction;
							if (IsStoredProcedure()) sqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									//SqlParameter param = sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null);
									//param.Direction = ParameterDirection.Output;
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							rtnValue = sqlCommand.ExecuteScalar();

							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									//GetReturnParameters().Set(key, sqlCommand.Parameters[key].Value);
									UpdateReturnParameter(key, sqlCommand.Parameters[key].Value);
								}
							}
							break;
#if NET_STD || NET_CORE || NET_FX || NET_STORE
						case SQL_TYPE.SQLITE:
							sqliteCommand = sqliteConnection.CreateCommand();
							sqliteCommand.CommandText = GetQuery();
							rtnValue = sqliteCommand.ExecuteScalar();
							break;
						case SQL_TYPE.POSTGRESQL:		// postgresql 접속 처리시
							//npgsqlCommand = new NpgsqlCommand(GetQuery(), npgsqlConnection);
							npgsqlCommand = npgsqlConnection.CreateCommand();
							npgsqlCommand.CommandText = GetQuery();
							if (npgsqlTransaction != null) npgsqlCommand.Transaction = npgsqlTransaction;
							if (IsStoredProcedure()) npgsqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							/*if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									npgsqlCommand.Parameters.AddWithValue(GetParameters().GetKey(cnti), GetParameters().Get(cnti));
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									npgsqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null).Direction = ParameterDirection.Output;
								}
							}*/
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									//SqlParameter param = sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null);
									//param.Direction = ParameterDirection.Output;
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							rtnValue = npgsqlCommand.ExecuteScalar();

							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									//GetReturnParameters().Set(key, npgsqlCommand.Parameters[key].Value);
									UpdateReturnParameter(key, npgsqlCommand.Parameters[key].Value);
								}
							}
							break;
						case SQL_TYPE.MYSQL:
							//mySqlCommand = new MySqlCommand (GetQuery(), mySqlConnection);
							mySqlCommand = mySqlConnection.CreateCommand();
							mySqlCommand.CommandText = GetQuery();
							if (mySqlTransaction != null) mySqlCommand.Transaction = mySqlTransaction;
							if (IsStoredProcedure()) mySqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							/*if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									mySqlCommand.Parameters.AddWithValue(GetParameters().GetKey(cnti), GetParameters().Get(cnti));
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									mySqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null).Direction = ParameterDirection.Output;
								}
							}*/
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									//SqlParameter param = sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null);
									//param.Direction = ParameterDirection.Output;
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							rtnValue = mySqlCommand.ExecuteScalar();

							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									//GetReturnParameters().Set(key, mySqlCommand.Parameters[key].Value);
									UpdateReturnParameter(key, mySqlCommand.Parameters[key].Value);
								}
							}
							break;
#endif
					}
				}
				else {
					throw new Exception("Exception occured in Get : Can not open connection!");
				}
			}
			catch (Exception ex) {
				if (sqlTransaction == null) {
					if (ex.InnerException != null) {
						throw new Exception("Exception in Get.Inner", ex.InnerException);
					}
					else {
						throw new Exception("Exception in Get", ex);
					}
				}
				else {
					//
					Exception exception_rollback = null;
					try {
						switch (this.GetSqlType()) {
							case AZSql.SQL_TYPE.MSSQL: sqlTransaction.Rollback(); break;
							case AZSql.SQL_TYPE.MYSQL: mySqlTransaction.Rollback(); break;
							case AZSql.SQL_TYPE.POSTGRESQL: npgsqlTransaction.Rollback(); break;
						}
					}
					catch (Exception ex_rollback) {
						exception_rollback = ex_rollback;
					}
					finally {
						RemoveTran();
						//
						if (ex != null && this.action_tran_on_commit != null) this.action_tran_on_commit(ex);
						if (exception_rollback != null && this.action_tran_on_rollback != null) this.action_tran_on_rollback(exception_rollback);
						//
						ClearTransCallback();
					}
				}
			}
			finally {
				switch (this.GetSqlType()) {
					case AZSql.SQL_TYPE.MSSQL: if (sqlTransaction == null) Close(); break;
					case AZSql.SQL_TYPE.MYSQL: if (mySqlTransaction == null) Close(); break;
					case AZSql.SQL_TYPE.POSTGRESQL: if (npgsqlTransaction == null) Close(); break;
				}
			}

			if ((sqlTransaction != null || mySqlTransaction != null || npgsqlTransaction != null) && transaction_result != null) {
				transaction_result.Add("Get." + (transaction_result.Size() + 1), rtnValue);
			}
			return rtnValue;
		}
		
#if NET_STD || NET_CORE || NET_FX || NET_STORE
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public async Task<object> GetAsync(string query) {
			SetQuery(query);
			return await GetAsync();
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="identity">쿼리 실행 후 발생되는 identity값 반환 여부 설정</param>
		public async Task<object> GetAsync(string query, AZData parameters) {
			SetQuery(query);
			SetParameters(parameters);
			return await GetAsync();
		}

		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public async Task<object> GetAsync() {
			object rtnValue = null;
			//
			if (in_transaction && !connected) return rtnValue;
			try {
				if (!connected) await OpenAsync();

				if (connected) {
					switch (this.db_info.SqlType) {
						case SQL_TYPE.MSSQL:		// mssql 접속 처리시
							sqlCommand = sqlConnection.CreateCommand();
							sqlCommand.CommandText = GetQuery();
							if (sqlTransaction != null) sqlCommand.Transaction = sqlTransaction;
							if (IsStoredProcedure()) sqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							rtnValue = await sqlCommand.ExecuteScalarAsync();
							//
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									UpdateReturnParameter(key, sqlCommand.Parameters[key].Value);
								}
							}
							break;
						case SQL_TYPE.SQLITE:
							sqliteCommand = sqliteConnection.CreateCommand();
							sqliteCommand.CommandText = GetQuery();
							rtnValue = await sqliteCommand.ExecuteScalarAsync();
							break;
						case SQL_TYPE.POSTGRESQL:		// postgresql 접속 처리시
							npgsqlCommand = npgsqlConnection.CreateCommand();
							npgsqlCommand.CommandText = GetQuery();
							if (npgsqlTransaction != null) npgsqlCommand.Transaction = npgsqlTransaction;
							if (IsStoredProcedure()) npgsqlCommand.CommandType = CommandType.StoredProcedure;
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							rtnValue = await npgsqlCommand.ExecuteScalarAsync();
							//
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									UpdateReturnParameter(key, npgsqlCommand.Parameters[key].Value);
								}
							}
							break;
						case SQL_TYPE.MYSQL:
							mySqlCommand = mySqlConnection.CreateCommand();
							mySqlCommand.CommandText = GetQuery();
							if (mySqlTransaction != null) mySqlCommand.Transaction = mySqlTransaction;
							if (IsStoredProcedure()) mySqlCommand.CommandType = CommandType.StoredProcedure;
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							rtnValue = await mySqlCommand.ExecuteScalarAsync();
							//
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									UpdateReturnParameter(key, mySqlCommand.Parameters[key].Value);
								}
							}
							break;
					}
				}
				else {
					throw new Exception("Exception occured in Get : Can not open connection!");
				}
			}
			catch (Exception ex) {
				if (sqlTransaction == null) {
					if (ex.InnerException != null) {
						throw new Exception("Exception in Get.Inner", ex.InnerException);
					}
					else {
						throw new Exception("Exception in Get", ex);
					}
				}
				else {
					//
					Exception exception_rollback = null;
					try {
						switch (this.GetSqlType()) {
							case AZSql.SQL_TYPE.MSSQL: sqlTransaction.Rollback(); break;
							case AZSql.SQL_TYPE.MYSQL: mySqlTransaction.Rollback(); break;
							case AZSql.SQL_TYPE.POSTGRESQL: npgsqlTransaction.Rollback(); break;
						}
					}
					catch (Exception ex_rollback) {
						exception_rollback = ex_rollback;
					}
					finally {
						RemoveTran();
						//
						if (ex != null && this.action_tran_on_commit != null) this.action_tran_on_commit(ex);
						if (exception_rollback != null && this.action_tran_on_rollback != null) this.action_tran_on_rollback(exception_rollback);
						//
						ClearTransCallback();
					}
				}
			}
			finally {
				switch (this.GetSqlType()) {
					case AZSql.SQL_TYPE.MSSQL: if (sqlTransaction == null) Close(); break;
					case AZSql.SQL_TYPE.MYSQL: if (mySqlTransaction == null) Close(); break;
					case AZSql.SQL_TYPE.POSTGRESQL: if (npgsqlTransaction == null) Close(); break;
				}
			}

			if ((sqlTransaction != null || mySqlTransaction != null || npgsqlTransaction != null) && transaction_result != null) {
				transaction_result.Add("Get." + (transaction_result.Size() + 1), rtnValue);
			}
			return rtnValue;
		}
#endif

		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		public object GetObject() {
			return Get();
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public object GetObject(string query) {
			return Get(query);
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="identity">쿼리 실행 후 발생되는 identity값 반환 여부 설정</param>
		public object GetObject(string query, AZData parameters) {
			return Get(query, parameters);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public int GetInt() {
			return AZString.Init(Get()).ToInt(0);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public int GetInt(int default_value) {
			return AZString.Init(Get()).ToInt(default_value);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public int GetInt(string query, int default_value) {
			return AZString.Init(Get(query)).ToInt(default_value);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public int GetInt(string query, AZData parameters, int default_value) {
			return AZString.Init(Get(query, parameters)).ToInt(default_value);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public int GetInt(string query) {
			return GetInt(query, 0);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public long GetLong() {
			return AZString.Init(Get()).ToLong(0);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public long GetLong(long default_value) {
			return AZString.Init(Get()).ToLong(default_value);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public long GetLong(string query) {
			return GetLong(query, 0);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public long GetLong(string query, long default_value) {
			return AZString.Init(Get(query)).ToLong(default_value);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public long GetLong(string query, AZData parameters, long default_value) {
			return AZString.Init(Get(query, parameters)).ToLong(default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public float GetFloat() {
			return AZString.Init(Get()).ToFloat(0f);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public float GetFloat(float p_default_value) {
			return AZString.Init(Get()).ToFloat(p_default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public float GetFloat(string query, float p_default_value) {
			return AZString.Init(Get(query)).ToFloat(p_default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public float GetFloat(string query, AZData parameters, float p_default_value) {
			return AZString.Init(Get(query, parameters)).ToFloat(p_default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public float GetFloat(string query) {
			return GetFloat(query, 0f);
		}
		/// <summary>단일 결과값을 string으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public string GetString() {
			return AZString.Init(Get()).String("");
		}
		/// <summary>단일 결과값을 string으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public string GetString(string query, string p_default_value) {
			return AZString.Init(Get(query)).String(p_default_value);
		}
		/// <summary>단일 결과값을 string으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public string GetString(string query, AZData parameters, string p_default_value) {
			return AZString.Init(Get(query, parameters)).String(p_default_value);
		}
		/// <summary>단일 결과값을 string으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public string GetString(string query) {
			return GetString();
		}

#if NET_STD || NET_CORE || NET_FX || NET_STORE
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<object> GetObjectAsync() {
			return await GetAsync();
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public async Task<object> GetObjectAsync(string query) {
			return await GetAsync(query);
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<object> GetObjectAsync(string query, AZData parameters) {
			return await GetAsync(query, parameters);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<int> GetIntAsync() {
			return AZString.Init(await GetAsync()).ToInt(0);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<int> GetIntAsync(int default_value) {
			return AZString.Init(await GetAsync()).ToInt(default_value);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<int> GetIntAsync(string query) {
			return await GetIntAsync(query, 0);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<int> GetIntAsync(string query, int default_value) {
			return AZString.Init(await GetAsync(query)).ToInt(default_value);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<int> GetIntAsync(string query, AZData parameters, int default_value) {
			return AZString.Init(await GetAsync(query, parameters)).ToInt(default_value);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<long> GetLongAsync() {
			return AZString.Init(await GetAsync()).ToLong(0);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<long> GetLongAsync(long default_value) {
			return AZString.Init(await GetAsync()).ToLong(default_value);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<long> GetLongAsync(string query) {
			return await GetLongAsync(query, 0);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<long> GetLongAsync(string query, long default_value) {
			return AZString.Init(await GetAsync(query)).ToLong(default_value);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<long> GetLongAsync(string query, AZData parameters, long default_value) {
			return AZString.Init(await GetAsync(query, parameters)).ToLong(default_value);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<float> GetFloatAsync() {
			return AZString.Init(await GetAsync()).ToFloat(0f);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<float> GetFloatAsync(float p_default_value) {
			return AZString.Init(await GetAsync()).ToFloat(p_default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<float> GetFloatAsync(string query, float p_default_value) {
			return AZString.Init(await GetAsync(query)).ToFloat(p_default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<float> GetFloatAsync(string query, AZData parameters, float p_default_value) {
			return AZString.Init(await GetAsync(query, parameters)).ToFloat(p_default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<float> GetFloatAsync(string query) {
			return await GetFloatAsync(query, 0f);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<string> GetStringAsync() {
			return AZString.Init(await GetAsync()).String("");
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<string> GetStringAsync(string query, string p_default_value) {
			return AZString.Init(await GetAsync(query)).String(p_default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<string> GetStringAsync(string query, AZData parameters, string p_default_value) {
			return AZString.Init(await GetAsync(query, parameters)).String(p_default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<string> GetStringAsync(string query) {
			return await GetStringAsync();
		}
#endif

		/// <summary>단일 행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public AZData GetData(string query) {
			SetQuery(query);
			return GetData();
		}
		/// <summary>단일 행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public AZData GetData(string query, AZData parameters) {
			SetQuery(query);
			SetParameters(parameters);
			return GetData();
		}
		/// <summary>단일 행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		public AZData GetData() {
			AZData rtnValue = new AZData ();
			if (in_transaction && !connected) return rtnValue;
			//
			SqlDataReader reader_mssql = null;
#if NET_STD || NET_CORE || NET_FX || NET_STORE
			MySqlDataReader reader_mysql = null;
			SqliteDataReader reader_sqlite = null;
			NpgsqlDataReader reader_npgsql = null;
#endif
			try {
				if (!connected) Open ();
				if (connected) {
					switch (this.db_info.SqlType) {
						case SQL_TYPE.MSSQL:		// mssql 접속 처리시
							//sqlCommand = new SqlCommand(p_query, sqlConnection);
							sqlCommand = sqlConnection.CreateCommand();
							sqlCommand.CommandText = GetQuery();
							if (sqlTransaction != null) sqlCommand.Transaction = sqlTransaction;
							if (IsStoredProcedure()) sqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							/*if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									sqlCommand.Parameters.AddWithValue(GetParameters().GetKey(cnti), GetParameters().Get(cnti));
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null).Direction = ParameterDirection.Output;
								}
							}*/
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									//SqlParameter param = sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null);
									//param.Direction = ParameterDirection.Output;
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							reader_mssql = sqlCommand.ExecuteReader();
							while (reader_mssql.Read()) {
								int colCnt = reader_mssql.FieldCount;
								for (int cnti = 0; cnti < colCnt; cnti++) {
									rtnValue.Add(reader_mssql.GetName(cnti), reader_mssql[cnti]);
								}
								break;
							}

							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									//GetReturnParameters().Set(key, sqlCommand.Parameters[key].Value);
									UpdateReturnParameter(key, sqlCommand.Parameters[key].Value);
								}
							}
							break;
#if NET_STD || NET_CORE || NET_FX || NET_STORE
						case SQL_TYPE.MYSQL:
							mySqlCommand = new MySqlCommand (GetQuery(), mySqlConnection);
							if (mySqlTransaction != null) mySqlCommand.Transaction = mySqlTransaction;
							if (IsStoredProcedure()) mySqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							/*if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									mySqlCommand.Parameters.AddWithValue(GetParameters().GetKey(cnti), GetParameters().Get(cnti));
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									mySqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null).Direction = ParameterDirection.Output;
								}
							}*/
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									//SqlParameter param = sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null);
									//param.Direction = ParameterDirection.Output;
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							reader_mysql = mySqlCommand.ExecuteReader();
							while (reader_mysql.Read()) {
								int colCnt = reader_mysql.FieldCount;

								for (int cnti = 0; cnti < colCnt; cnti++) {
									rtnValue.Add (reader_mysql.GetName (cnti), reader_mysql [cnti]);
								}
								break;
							}
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									//GetReturnParameters().Set(key, mySqlCommand.Parameters[key].Value);
									UpdateReturnParameter(key, mySqlCommand.Parameters[key].Value);
								}
							}
							break;
						case SQL_TYPE.SQLITE:
							sqliteCommand = sqliteConnection.CreateCommand();
							sqliteCommand.CommandText = GetQuery();
							reader_sqlite = sqliteCommand.ExecuteReader();

							while (reader_sqlite.Read()) {
								int colCnt = reader_sqlite.FieldCount;

								for (int cnti = 0; cnti < colCnt; cnti++) {
									rtnValue.Add (reader_sqlite.GetName (cnti), reader_sqlite [cnti]);
								}
								break;
							}
							break;
						case SQL_TYPE.POSTGRESQL:		// postgresql 접속 처리시
							npgsqlCommand = new NpgsqlCommand(GetQuery(), npgsqlConnection);
							if (npgsqlTransaction != null) npgsqlCommand.Transaction = npgsqlTransaction;
							if (IsStoredProcedure()) npgsqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							/*if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									npgsqlCommand.Parameters.AddWithValue(GetParameters().GetKey(cnti), GetParameters().Get(cnti));
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									npgsqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null).Direction = ParameterDirection.Output;
								}
							}*/
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									//SqlParameter param = sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null);
									//param.Direction = ParameterDirection.Output;
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							reader_npgsql = npgsqlCommand.ExecuteReader();
							while (reader_npgsql.Read()) {
								int colCnt = reader_npgsql.FieldCount;

								for (int cnti = 0; cnti < colCnt; cnti++) {
									rtnValue.Add(reader_npgsql.GetName(cnti), reader_npgsql[cnti]);
								}
								break;
							}

							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									//GetReturnParameters().Set(key, npgsqlCommand.Parameters[key].Value);
									UpdateReturnParameter(key, npgsqlCommand.Parameters[key].Value);
								}
							}
							break;
#endif
					}
				}
			}
			catch (Exception ex) {
				if (this.GetSqlType() == AZSql.SQL_TYPE.MSSQL && sqlTransaction == null ||
					this.GetSqlType() == AZSql.SQL_TYPE.MYSQL && mySqlTransaction == null ||
					this.GetSqlType() == AZSql.SQL_TYPE.POSTGRESQL && npgsqlTransaction == null) {
					if (ex.InnerException != null) {
						throw new Exception("Exception in GetData.Inner", ex.InnerException);
					}
					else {
						throw new Exception("Exception in GetData", ex);
					}
				}
				else {
					//
					Exception exception_rollback = null;
					try {
						if (connected) {
							switch (this.GetSqlType()) {
								case AZSql.SQL_TYPE.MSSQL: sqlTransaction.Rollback(); break;
								case AZSql.SQL_TYPE.MYSQL: mySqlTransaction.Rollback(); break;
								case AZSql.SQL_TYPE.POSTGRESQL: npgsqlTransaction.Rollback(); break;
							}
						}
					}
					catch (Exception ex_rollback) {
						exception_rollback = ex_rollback;
					}
					finally {
						RemoveTran();
						//
						if (this.action_tran_on_commit != null) {
							this.action_tran_on_commit(ex);
						}
						if (exception_rollback != null && this.action_tran_on_rollback != null) {
							this.action_tran_on_rollback(exception_rollback);
						}
						//
						ClearTransCallback();
					}
				}
			}
			finally {
				if (reader_mssql != null) reader_mssql.Dispose();
#if NET_STD || NET_CORE || NET_FX || NET_STORE
				if (reader_mysql != null) reader_mysql.Dispose ();
				if (reader_sqlite != null) reader_sqlite.Dispose ();
				if (reader_npgsql != null) reader_npgsql.Dispose();
#endif
				switch (this.GetSqlType()) {
					case AZSql.SQL_TYPE.MSSQL: if (sqlTransaction == null) Close(); break;
					case AZSql.SQL_TYPE.MYSQL: if (mySqlTransaction == null) Close(); break;
					case AZSql.SQL_TYPE.POSTGRESQL: if (npgsqlTransaction == null) Close(); break;
				}
			}
			if ((sqlTransaction != null || mySqlTransaction != null || npgsqlTransaction != null) && 
				transaction_result != null) {
				transaction_result.Add("GetData." + (transaction_result.Size() + 1), rtnValue);
			}
			return rtnValue;
		}

#if NET_STD || NET_CORE || NET_FX || NET_STORE
		/// <summary>단일 행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public async Task<AZData> GetDataAsync(string query) {
			SetQuery(query);
			return await GetDataAsync();
		}
		/// <summary>단일 행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public async Task<AZData> GetDataAsync(string query, AZData parameters) {
			SetQuery(query);
			SetParameters(parameters);
			return await GetDataAsync();
		}
		/// <summary>단일 행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<AZData> GetDataAsync() {
			AZData rtnValue = new AZData ();
			//
			if (in_transaction && !connected) return rtnValue;
			//
			SqlDataReader reader_mssql = null;
			DbDataReader reader_mysql = null;
			SqliteDataReader reader_sqlite = null;
			DbDataReader reader_npgsql = null;
			try {
				if (!connected) await OpenAsync();
				if (connected) {
					switch (this.db_info.SqlType) {
						case SQL_TYPE.MSSQL:		// mssql 접속 처리시
							sqlCommand = sqlConnection.CreateCommand();
							sqlCommand.CommandText = GetQuery();
							if (sqlTransaction != null) sqlCommand.Transaction = sqlTransaction;
							if (IsStoredProcedure()) sqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							reader_mssql = await sqlCommand.ExecuteReaderAsync();
							while (await reader_mssql.ReadAsync()) {
								int colCnt = reader_mssql.FieldCount;
								for (int cnti = 0; cnti < colCnt; cnti++) {
									rtnValue.Add(reader_mssql.GetName(cnti), reader_mssql[cnti]);
								}
								break;
							}
							//
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									UpdateReturnParameter(key, sqlCommand.Parameters[key].Value);
								}
							}
							break;
						case SQL_TYPE.MYSQL:
							//mySqlCommand = new MySqlCommand (GetQuery(), mySqlConnection);
							mySqlCommand = mySqlConnection.CreateCommand();
							mySqlCommand.CommandText = GetQuery();
							if (mySqlTransaction != null) mySqlCommand.Transaction = mySqlTransaction;
							if (IsStoredProcedure()) mySqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							reader_mysql = await mySqlCommand.ExecuteReaderAsync();
							while (await reader_mysql.ReadAsync()) {
								int colCnt = reader_mysql.FieldCount;
								for (int cnti = 0; cnti < colCnt; cnti++) {
									rtnValue.Add(reader_mysql.GetName (cnti), reader_mysql [cnti]);
								}
								break;
							}
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									UpdateReturnParameter(key, mySqlCommand.Parameters[key].Value);
								}
							}
							break;
						case SQL_TYPE.SQLITE:
							sqliteCommand = sqliteConnection.CreateCommand();
							sqliteCommand.CommandText = GetQuery();
							reader_sqlite = await sqliteCommand.ExecuteReaderAsync();
							//
							while (await reader_sqlite.ReadAsync()) {
								int colCnt = reader_sqlite.FieldCount;
								for (int cnti = 0; cnti < colCnt; cnti++) {
									rtnValue.Add (reader_sqlite.GetName (cnti), reader_sqlite [cnti]);
								}
								break;
							}
							break;
						case SQL_TYPE.POSTGRESQL:		// postgresql 접속 처리시
							//npgsqlCommand = new NpgsqlCommand(GetQuery(), npgsqlConnection);
							npgsqlCommand = npgsqlConnection.CreateCommand();
							npgsqlCommand.CommandText = GetQuery();
							if (npgsqlTransaction != null) npgsqlCommand.Transaction = npgsqlTransaction;
							if (IsStoredProcedure()) npgsqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							reader_npgsql = await npgsqlCommand.ExecuteReaderAsync();
							while (await reader_npgsql.ReadAsync()) {
								int colCnt = reader_npgsql.FieldCount;
								for (int cnti = 0; cnti < colCnt; cnti++) {
									rtnValue.Add(reader_npgsql.GetName(cnti), reader_npgsql[cnti]);
								}
								break;
							}
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									UpdateReturnParameter(key, npgsqlCommand.Parameters[key].Value);
								}
							}
							break;
					}
				}
			}
			catch (Exception ex) {
				if (this.GetSqlType() == AZSql.SQL_TYPE.MSSQL && sqlTransaction == null ||
					this.GetSqlType() == AZSql.SQL_TYPE.MYSQL && mySqlTransaction == null ||
					this.GetSqlType() == AZSql.SQL_TYPE.POSTGRESQL && npgsqlTransaction == null) {
					if (ex.InnerException != null) {
						throw new Exception("Exception in GetData.Inner", ex.InnerException);
					}
					else {
						throw new Exception("Exception in GetData", ex);
					}
				}
				else {
					//
					Exception exception_rollback = null;
					try {
						if (connected) {
							switch (this.GetSqlType()) {
								case AZSql.SQL_TYPE.MSSQL: sqlTransaction.Rollback(); break;
								case AZSql.SQL_TYPE.MYSQL: mySqlTransaction.Rollback(); break;
								case AZSql.SQL_TYPE.POSTGRESQL: npgsqlTransaction.Rollback(); break;
							}
						}
					}
					catch (Exception ex_rollback) {
						exception_rollback = ex_rollback;
					}
					finally {
						RemoveTran();
						//
						if (this.action_tran_on_commit != null) this.action_tran_on_commit(ex);
						if (exception_rollback != null && this.action_tran_on_rollback != null) this.action_tran_on_rollback(exception_rollback);
						//
						ClearTransCallback();
					}
				}
			}
			finally {
				if (reader_mssql != null) reader_mssql.Dispose();
				if (reader_mysql != null) reader_mysql.Dispose ();
				if (reader_sqlite != null) reader_sqlite.Dispose ();
				if (reader_npgsql != null) reader_npgsql.Dispose();
				switch (this.GetSqlType()) {
					case AZSql.SQL_TYPE.MSSQL: if (sqlTransaction == null) Close(); break;
					case AZSql.SQL_TYPE.MYSQL: if (mySqlTransaction == null) Close(); break;
					case AZSql.SQL_TYPE.POSTGRESQL: if (npgsqlTransaction == null) Close(); break;
				}
			}
			if ((sqlTransaction != null || mySqlTransaction != null || npgsqlTransaction != null) && transaction_result != null) {
				transaction_result.Add("GetData." + (transaction_result.Size() + 1), rtnValue);
			}
			return rtnValue;
		}
#endif
				
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		public AZList GetList() {
			return GetList(0, -1);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public AZList GetList(string query) {
			SetQuery(query);
			return GetList(0);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		public AZList GetList(string query, int offset) {
			SetQuery(query);
			return GetList(offset);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		/// <param name="length">반환받을 행의 수 지정</param>
		public AZList GetList(string query, int offset, int length) {
			SetQuery(query);
			return GetList(offset, length);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		public AZList GetList(string query, AZData parameters) {
			SetQuery(query);
			SetParameters(parameters);
			return GetList();
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		public AZList GetList(string query, AZData parameters, int offset) {
			SetQuery(query);
			SetParameters(parameters);
			return GetList(offset);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		/// <param name="length">반환받을 행의 수 지정</param>
		public AZList GetList(string query, AZData parameters, int offset, int length) {
			SetQuery(query);
			SetParameters(parameters);
			return GetList(offset, length);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		public AZList GetList(int offset) {
			return GetList(offset, -1);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		/// <param name="length">반환받을 행의 수 지정</param>
		public AZList GetList(int offset, int length) {
			AZList rtnValue = new AZList ();
			if (in_transaction && !connected) return rtnValue;
			//
			SqlDataReader reader_mssql = null;
#if NET_STD || NET_CORE || NET_FX || NET_STORE
			MySqlDataReader reader_mysql = null;
			SqliteDataReader reader_sqlite = null;
			NpgsqlDataReader reader_npgsql = null;
#endif
			try {
				if (!connected) Open ();
				int idx;
				if (connected) {
					switch (this.db_info.SqlType) {
						case SQL_TYPE.MSSQL:		// mssql 접속 처리시
							//sqlCommand = new SqlCommand(p_query, sqlConnection);
							sqlCommand = sqlConnection.CreateCommand();
							sqlCommand.CommandText = GetQuery();
							if (sqlTransaction != null) sqlCommand.Transaction = sqlTransaction;
							if (IsStoredProcedure()) sqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							/*if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									sqlCommand.Parameters.AddWithValue(GetParameters().GetKey(cnti), GetParameters().Get(cnti));
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null).Direction = ParameterDirection.Output;
								}
							}*/
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									//SqlParameter param = sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null);
									//param.Direction = ParameterDirection.Output;
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							reader_mssql = sqlCommand.ExecuteReader();
							
							idx = 0;		// for check offset
							while (reader_mssql.Read()) {
								if (idx < offset) {	 // 시작점보다 작으면 다음으로.
									idx++;	// offset check value update
									continue;
								}
								if (length > 0 && idx >= (offset + length)) {	// 시작점 + 길이 보다 크면 종료
									break;
								}
								int colCnt = reader_mssql.FieldCount;
								AZData data = new AZData();

								for (int cnti = 0; cnti < colCnt; cnti++) {
									data.Add(reader_mssql.GetName(cnti), reader_mssql[cnti]);
								}
								rtnValue.Add(data);
								idx++;	// offset check value update
							}

							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									//GetReturnParameters().Set(key, sqlCommand.Parameters[key].Value);
									UpdateReturnParameter(key, sqlCommand.Parameters[key].Value);
								}
							}
							break;
#if NET_STD || NET_CORE || NET_FX || NET_STORE
						case SQL_TYPE.SQLITE:
							sqliteCommand = sqliteConnection.CreateCommand();
							sqliteCommand.CommandText = GetQuery();
							reader_sqlite = sqliteCommand.ExecuteReader();
													
							idx = 0;		// for check offset
							while (reader_sqlite.Read()) {
								if (idx < offset) {	 // 시작점보다 작으면 다음으로.
									idx++;	// offset check value update
									continue;
								}
								if (length > 0 && idx >= (offset + length)) {	// 시작점 + 길이 보다 크면 종료
									break;
								}
								int colCnt = reader_sqlite.FieldCount;
								AZData data = new AZData ();

								for (int cnti = 0; cnti < colCnt; cnti++) {
									data.Add (reader_sqlite.GetName (cnti), reader_sqlite [cnti]);
								}
								rtnValue.Add(data);
								idx++;	// offset check value update
							}
							break;
						case SQL_TYPE.POSTGRESQL:		// postgresql 접속 처리시
							npgsqlCommand = new NpgsqlCommand(GetQuery(), npgsqlConnection);
							if (npgsqlTransaction != null) npgsqlCommand.Transaction = npgsqlTransaction;
							if (IsStoredProcedure()) npgsqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							/*if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									npgsqlCommand.Parameters.AddWithValue(GetParameters().GetKey(cnti), GetParameters().Get(cnti));
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									npgsqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null).Direction = ParameterDirection.Output;
								}
							}*/
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									//SqlParameter param = sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null);
									//param.Direction = ParameterDirection.Output;
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							reader_npgsql = npgsqlCommand.ExecuteReader();
							
							idx = 0;		// for check offset
							while (reader_npgsql.Read()) {
								if (idx < offset) {	 // 시작점보다 작으면 다음으로.
									idx++;	// offset check value update
									continue;
								}
								if (length > 0 && idx >= (offset + length)) {	// 시작점 + 길이 보다 크면 종료
									break;
								}
								int colCnt = reader_npgsql.FieldCount;
								AZData data = new AZData();

								for (int cnti = 0; cnti < colCnt; cnti++) {
									data.Add(reader_npgsql.GetName(cnti), reader_npgsql[cnti]);
								}
								rtnValue.Add(data);
								idx++;	// offset check value update
							}

							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									//GetReturnParameters().Set(key, npgsqlCommand.Parameters[key].Value);
									UpdateReturnParameter(key, npgsqlCommand.Parameters[key].Value);
								}
							}
							break;
						case SQL_TYPE.MYSQL:
							mySqlCommand = new MySqlCommand (GetQuery(), mySqlConnection);
							if (mySqlTransaction != null) mySqlCommand.Transaction = mySqlTransaction;
							if (IsStoredProcedure()) mySqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							/*if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									mySqlCommand.Parameters.AddWithValue(GetParameters().GetKey(cnti), GetParameters().Get(cnti));
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									mySqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null).Direction = ParameterDirection.Output;
								}
							}*/
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									//SqlParameter param = sqlCommand.Parameters.AddWithValue(GetReturnParameters().GetKey(cnti), null);
									//param.Direction = ParameterDirection.Output;
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							reader_mysql = mySqlCommand.ExecuteReader();

							idx = 0;		// for check offset
							while (reader_mysql.Read()) {
								if (idx < offset) {	 // 시작점보다 작으면 다음으로.
									idx++;	// offset check value update
									continue;
								}
								if (length > 0 && idx >= (offset + length)) {	// 시작점 + 길이 보다 크면 종료
									break;
								}
								int colCnt = reader_mysql.FieldCount;
								AZData data = new AZData ();

								for (int cnti = 0; cnti < colCnt; cnti++) {
									data.Add (reader_mysql.GetName (cnti), reader_mysql[cnti]);
								}
								rtnValue.Add (data);
								idx++;	// offset check value update
							}

							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									//GetReturnParameters().Set(key, mySqlCommand.Parameters[key].Value);
									UpdateReturnParameter(key, mySqlCommand.Parameters[key].Value);
								}
							}
							break;
#endif
					}
				}
				else {
					throw new Exception("Exception occured in GetList : Can not open connection!");
				}
			}
			catch (Exception ex) {
				if (this.GetSqlType() == AZSql.SQL_TYPE.MSSQL && sqlTransaction == null ||
					this.GetSqlType() == AZSql.SQL_TYPE.MYSQL && mySqlTransaction == null ||
					this.GetSqlType() == AZSql.SQL_TYPE.POSTGRESQL && npgsqlTransaction == null) {
					if (ex.InnerException != null) {
						throw new Exception("Exception in GetList.Inner", ex.InnerException);
					}
					else {
						throw new Exception("Exception in GetList", ex);
					}
				}
				else {
					//
					Exception exception_rollback = null;
					try {
						if (connected) {
							switch (this.GetSqlType()) {
								case AZSql.SQL_TYPE.MSSQL: sqlTransaction.Rollback(); break;
								case AZSql.SQL_TYPE.MYSQL: mySqlTransaction.Rollback(); break;
								case AZSql.SQL_TYPE.POSTGRESQL: npgsqlTransaction.Rollback(); break;
							}
						}
					}
					catch (Exception ex_rollback) {
						exception_rollback = ex_rollback;
					}
					finally {
						RemoveTran();
						//
						if (this.action_tran_on_commit != null) {
							this.action_tran_on_commit(ex);
						}
						if (exception_rollback != null && this.action_tran_on_rollback != null) {
							this.action_tran_on_rollback(exception_rollback);
						}
						//
						ClearTransCallback();
					}
				}
			}
			finally {
				if (reader_mssql != null) reader_mssql.Dispose();
#if NET_STD || NET_CORE || NET_FX || NET_STORE
				if (reader_mysql != null) reader_mysql.Dispose();
				if (reader_sqlite != null) reader_sqlite.Dispose();
				if (reader_npgsql != null) reader_npgsql.Dispose();
#endif
				switch (this.GetSqlType()) {
					case AZSql.SQL_TYPE.MSSQL: if (sqlTransaction == null) Close (); break;
					case AZSql.SQL_TYPE.MYSQL: if (mySqlTransaction == null) Close (); break;
					case AZSql.SQL_TYPE.POSTGRESQL: if (npgsqlTransaction == null) Close (); break;
				}
			}

			if ((sqlTransaction != null || mySqlTransaction != null || npgsqlTransaction != null) && transaction_result != null) {
				transaction_result.Add("GetList." + (transaction_result.Size() + 1), rtnValue);
			}
			return rtnValue;
		}

#if NET_STD || NET_CORE || NET_FX || NET_STORE
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<AZList> GetListAsync() {
			return await GetListAsync(0, -1);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public async Task<AZList> GetListAsync(string query) {
			SetQuery(query);
			return await GetListAsync(0);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		public async Task<AZList> GetListAsync(string query, int offset) {
			SetQuery(query);
			return await GetListAsync(offset);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		/// <param name="length">반환받을 행의 수 지정</param>
		public async Task<AZList> GetListAsync(string query, int offset, int length) {
			SetQuery(query);
			return await GetListAsync(offset, length);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		public async Task<AZList> GetListAsync(string query, AZData parameters) {
			SetQuery(query);
			SetParameters(parameters);
			return await GetListAsync();
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		public async Task<AZList> GetListAsync(string query, AZData parameters, int offset) {
			SetQuery(query);
			SetParameters(parameters);
			return await GetListAsync(offset);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		/// <param name="length">반환받을 행의 수 지정</param>
		public async Task<AZList> GetListAsync(string query, AZData parameters, int offset, int length) {
			SetQuery(query);
			SetParameters(parameters);
			return await GetListAsync(offset, length);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		public async Task<AZList> GetListAsync(int offset) {
			return await GetListAsync(offset, -1);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		/// <param name="length">반환받을 행의 수 지정</param>
		public async Task<AZList> GetListAsync(int offset, int length) {
			AZList rtnValue = new AZList ();
			//
			if (in_transaction && !connected) return rtnValue;
			//
			SqlDataReader reader_mssql = null;
			DbDataReader reader_mysql = null;
			SqliteDataReader reader_sqlite = null;
			DbDataReader reader_npgsql = null;
			//
			try {
				if (!connected) await OpenAsync();
				int idx;
				if (connected) {
					switch (this.db_info.SqlType) {
						case SQL_TYPE.MSSQL:		// mssql 접속 처리시
							sqlCommand = sqlConnection.CreateCommand();
							sqlCommand.CommandText = GetQuery();
							if (sqlTransaction != null) sqlCommand.Transaction = sqlTransaction;
							if (IsStoredProcedure()) sqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									SqlParameter sqlParam = sqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.SqlDbType = paramData.GetSqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							reader_mssql = await sqlCommand.ExecuteReaderAsync();
							//
							idx = 0;		// for check offset
							while (await reader_mssql.ReadAsync()) {
								if (idx < offset) { idx++; continue; } // 시작점보다 작으면 다음으로.
								if (length > 0 && idx >= (offset + length)) break; // 시작점 + 길이 보다 크면 종료
								//
								int colCnt = reader_mssql.FieldCount;
								AZData data = new AZData();
								for (int cnti = 0; cnti < colCnt; cnti++) {
									data.Add(reader_mssql.GetName(cnti), reader_mssql[cnti]);
								}
								rtnValue.Add(data);
								idx++;	// offset check value update
							}
							//
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									UpdateReturnParameter(key, sqlCommand.Parameters[key].Value);
								}
							}
							break;
						case SQL_TYPE.SQLITE:
							sqliteCommand = sqliteConnection.CreateCommand();
							sqliteCommand.CommandText = GetQuery();
							reader_sqlite = await sqliteCommand.ExecuteReaderAsync();
							//
							idx = 0;		// for check offset
							while (await reader_sqlite.ReadAsync()) {
								if (idx < offset) { idx++; continue; } // 시작점보다 작으면 다음으로.
								if (length > 0 && idx >= (offset + length)) break;	// 시작점 + 길이 보다 크면 종료
								//
								int colCnt = reader_sqlite.FieldCount;
								AZData data = new AZData ();
								for (int cnti = 0; cnti < colCnt; cnti++) {
									data.Add (reader_sqlite.GetName (cnti), reader_sqlite [cnti]);
								}
								rtnValue.Add(data);
								idx++;	// offset check value update
							}
							break;
						case SQL_TYPE.POSTGRESQL:		// postgresql 접속 처리시
							//npgsqlCommand = new NpgsqlCommand(GetQuery(), npgsqlConnection);
							npgsqlCommand = npgsqlConnection.CreateCommand();
							npgsqlCommand.CommandText = GetQuery();
							if (npgsqlTransaction != null) npgsqlCommand.Transaction = npgsqlTransaction;
							if (IsStoredProcedure()) npgsqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									NpgsqlParameter sqlParam = npgsqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.NpgsqlDbType = paramData.GetNpgsqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							reader_npgsql = await npgsqlCommand.ExecuteReaderAsync();
							//
							idx = 0;		// for check offset
							while (await reader_npgsql.ReadAsync()) {
								if (idx < offset) { idx++; continue; } // 시작점보다 작으면 다음으로.
								if (length > 0 && idx >= (offset + length)) break;	// 시작점 + 길이 보다 크면 종료
								//
								int colCnt = reader_npgsql.FieldCount;
								AZData data = new AZData();
								for (int cnti = 0; cnti < colCnt; cnti++) {
									data.Add(reader_npgsql.GetName(cnti), reader_npgsql[cnti]);
								}
								rtnValue.Add(data);
								idx++;	// offset check value update
							}
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									UpdateReturnParameter(key, npgsqlCommand.Parameters[key].Value);
								}
							}
							break;
						case SQL_TYPE.MYSQL:
							mySqlCommand = new MySqlCommand (GetQuery(), mySqlConnection);
							if (mySqlTransaction != null) mySqlCommand.Transaction = mySqlTransaction;
							if (IsStoredProcedure()) mySqlCommand.CommandType = CommandType.StoredProcedure;
							// parameter 값이 지정된 경우에 한해서 처리
							if (GetParameters() != null) {
								for (int cnti=0; cnti<GetParameters().Size(); cnti++) {
									string key = GetParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							if (GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									ParameterData paramData = (ParameterData)GetReturnParameters().Get(cnti);
									MySqlParameter sqlParam = mySqlCommand.Parameters.AddWithValue(key, paramData.Value);
									sqlParam.Direction = ParameterDirection.Output;
									if (paramData.DbType != null) sqlParam.MySqlDbType = paramData.GetMySqlDbType();
									if (paramData.Size.HasValue) sqlParam.Size = paramData.Size.Value;
								}
							}
							reader_mysql = await mySqlCommand.ExecuteReaderAsync();
							//
							idx = 0;		// for check offset
							while (await reader_mysql.ReadAsync()) {
								if (idx < offset) { idx++; continue; }	// 시작점보다 작으면 다음으로.
								if (length > 0 && idx >= (offset + length)) break;	// 시작점 + 길이 보다 크면 종료
								//
								int colCnt = reader_mysql.FieldCount;
								AZData data = new AZData ();
								for (int cnti = 0; cnti < colCnt; cnti++) {
									data.Add(reader_mysql.GetName (cnti), reader_mysql[cnti]);
								}
								rtnValue.Add(data);
								idx++;	// offset check value update
							}
							//
							if (IsStoredProcedure() && GetReturnParameters() != null) {
								for (int cnti=0; cnti<GetReturnParameters().Size(); cnti++) {
									string key = GetReturnParameters().GetKey(cnti);
									UpdateReturnParameter(key, mySqlCommand.Parameters[key].Value);
								}
							}
							break;
					}
				}
				else {
					throw new Exception("Exception occured in GetList : Can not open connection!");
				}
			}
			catch (Exception ex) {
				if (this.GetSqlType() == AZSql.SQL_TYPE.MSSQL && sqlTransaction == null ||
					this.GetSqlType() == AZSql.SQL_TYPE.MYSQL && mySqlTransaction == null ||
					this.GetSqlType() == AZSql.SQL_TYPE.POSTGRESQL && npgsqlTransaction == null) {
					if (ex.InnerException != null) {
						throw new Exception("Exception in GetList.Inner", ex.InnerException);
					}
					else {
						throw new Exception("Exception in GetList", ex);
					}
				}
				else {
					//
					Exception exception_rollback = null;
					try {
						if (connected) {
							switch (this.GetSqlType()) {
								case AZSql.SQL_TYPE.MSSQL: sqlTransaction.Rollback(); break;
								case AZSql.SQL_TYPE.MYSQL: mySqlTransaction.Rollback(); break;
								case AZSql.SQL_TYPE.POSTGRESQL: npgsqlTransaction.Rollback(); break;
							}
						}
					}
					catch (Exception ex_rollback) {
						exception_rollback = ex_rollback;
					}
					finally {
						RemoveTran();
						//
						if (this.action_tran_on_commit != null) this.action_tran_on_commit(ex);
						if (exception_rollback != null && this.action_tran_on_rollback != null) {
							this.action_tran_on_rollback(exception_rollback);
						}
						//
						ClearTransCallback();
					}
				}
			}
			finally {
				if (reader_mssql != null) reader_mssql.Dispose();
				if (reader_mysql != null) reader_mysql.Dispose();
				if (reader_sqlite != null) reader_sqlite.Dispose();
				if (reader_npgsql != null) reader_npgsql.Dispose();
				switch (this.GetSqlType()) {
					case AZSql.SQL_TYPE.MSSQL: if (sqlTransaction == null) Close (); break;
					case AZSql.SQL_TYPE.MYSQL: if (mySqlTransaction == null) Close (); break;
					case AZSql.SQL_TYPE.POSTGRESQL: if (npgsqlTransaction == null) Close (); break;
				}
			}
			if ((sqlTransaction != null || mySqlTransaction != null || npgsqlTransaction != null) && transaction_result != null) {
				transaction_result.Add("GetList." + (transaction_result.Size() + 1), rtnValue);
			}
			return rtnValue;
		}
#endif

#if NET_STD || NET_CORE || NET_FX || NET_STORE
		/// <summary>설정된 연결 정보를 통해 DB연결 비동기 처리</summary>
		private async Task<bool> OpenAsync() {
			bool rtnValue = false;

			switch (this.db_info.SqlType) {
			case SQL_TYPE.MSSQL:
				sqlConnection = new SqlConnection(this.db_info.ConnectionString);
				await sqlConnection.OpenAsync();
				rtnValue = true;
				break;
			case SQL_TYPE.MYSQL:
				mySqlConnection = new MySqlConnection(this.db_info.ConnectionString);
				await mySqlConnection.OpenAsync();
				rtnValue = true;
				break;
			case SQL_TYPE.SQLITE:
				sqliteConnection = new SqliteConnection(this.db_info.ConnectionString);
				await sqliteConnection.OpenAsync();
				rtnValue = true;
				break;
			case SQL_TYPE.POSTGRESQL:
				npgsqlConnection = new NpgsqlConnection(this.db_info.ConnectionString);
				await npgsqlConnection.OpenAsync();
				rtnValue = true;
				break;
			}
			connected = rtnValue;
			return rtnValue;
		}
#endif
		/// <summary>설정된 연결 정보를 통해 DB연결 처리</summary>
		private bool Open() {
			bool rtnValue = false;

			switch (this.db_info.SqlType) {
			case SQL_TYPE.MSSQL:
				sqlConnection = new SqlConnection(this.db_info.ConnectionString);
				sqlConnection.Open ();
				rtnValue = true;
				break;
#if NET_STD || NET_CORE || NET_FX || NET_STORE
			case SQL_TYPE.MYSQL:
				mySqlConnection = new MySqlConnection(this.db_info.ConnectionString);
				mySqlConnection.Open ();
				rtnValue = true;
				break;
			case SQL_TYPE.SQLITE:
				sqliteConnection = new SqliteConnection(this.db_info.ConnectionString);
				sqliteConnection.Open ();
				rtnValue = true;
				break;
			case SQL_TYPE.POSTGRESQL:
				npgsqlConnection = new NpgsqlConnection(this.db_info.ConnectionString);
				npgsqlConnection.Open ();
				rtnValue = true;
				break;
#endif
			}
			connected = rtnValue;
			return rtnValue;
		}

		/// <summary>DB연결 종료 처리</summary>
		private bool Close() {
			bool rtnValue = false;

			switch (this.db_info.SqlType) {
			case SQL_TYPE.MSSQL:
				if (sqlConnection != null && sqlConnection.State.Equals (System.Data.ConnectionState.Open)) {
					sqlConnection.Close ();
				}
				sqlConnection = null;
				if (sqlCommand != null) sqlCommand.Dispose ();
				sqlCommand = null;
				rtnValue = true;
				break;
#if NET_STD || NET_CORE || NET_FX || NET_STORE
			case SQL_TYPE.MYSQL:
				if (mySqlConnection != null && mySqlConnection.State.Equals (System.Data.ConnectionState.Open)) {
					mySqlConnection.Close ();
				}
				mySqlConnection.Dispose ();
				mySqlConnection = null;
				mySqlCommand.Dispose ();
				mySqlCommand = null;
				rtnValue = true;
				break;
			case SQL_TYPE.SQLITE:
				if (sqliteConnection != null && sqliteConnection.State.Equals (System.Data.ConnectionState.Open)) {
					sqliteConnection.Close ();
				}
				sqliteConnection.Dispose ();
				sqliteConnection = null;
				if (sqliteCommand != null) sqliteCommand.Dispose ();
				sqliteCommand.Dispose ();
				sqliteCommand = null;
				rtnValue = true;
				break;
			case SQL_TYPE.POSTGRESQL:
				if (npgsqlConnection != null && npgsqlConnection.State.Equals (System.Data.ConnectionState.Open)) {
					npgsqlConnection.Close ();
				}
				npgsqlConnection = null;
				if (npgsqlCommand != null) npgsqlCommand.Dispose ();
				npgsqlCommand = null;
				rtnValue = true;
				break;
#endif
			}
			connected = !rtnValue;
			return rtnValue;
		}

		/// <summary>DB 연결 정보 저장용 객체</summary>
		public class DBConnectionInfo {
			/// <summary>기본 생성자</summary>
			public DBConnectionInfo(string p_json) {
				p_json = p_json.Trim();
				if (!p_json.StartsWith ("{") || !p_json.EndsWith("}")) {
					throw new Exception ("parameter must be json text.");
				}
				AZData data = AZString.JSON.ToAZData (p_json);

				string data_sql_type = data.GetString ("sql_type");

				string data_server = data.GetString ("server");
				int data_port = data.GetInt("port");
				string data_id = data.GetString ("id");
				string data_pw = data.GetString ("pw");
				string data_catalog = data.GetString ("catalog");

				string data_connection_string = data.GetString ("connection_string");

				if (data_sql_type.Length < 1) {
					throw new Exception ("sql_type not exist.");
					//return;
				}

				SqlType = GetSqlType(data_sql_type);

				if (data_connection_string.Length > 0) {
					ConnectionString = data_connection_string;

					if (SqlType.Equals(SQL_TYPE.SQLITE) && !ConnectionString.ToLower().StartsWith("data source=")) {
						ConnectionString = "Data Source=" + ConnectionString;
					}
				} 
				else {
					if (SqlType.Equals (SQL_TYPE.SQLITE) && data_server.Length < 1) {
						throw new Exception ("parameters not exist.");
					}
					else if (!SqlType.Equals (SQL_TYPE.SQLITE) && 
						(data_server.Length < 1 || data_port < 0 || data_id.Length < 1 ||
							data_pw.Length < 1 || data_catalog.Length < 1)) {
						throw new Exception ("parameters not exist.");
						//return;
					}

					Server = data_server;
					Port = data_port;
					ID = data_id;
					PW = data_pw;
					Catalog = data_catalog;

					switch (SqlType) {
					case SQL_TYPE.MSSQL:
						ConnectionString = "server=" + Server + ";" + (Port > 0 ? ":" + Port : "") + ";" +
							"uid=" + ID + ";" + "pwd=" + PW + ";" + "database=" + Catalog + ";";
						break;
					case SQL_TYPE.SQLITE:
						ConnectionString = "Data Source=" + Server;
						break;
					case SQL_TYPE.MYSQL:
						ConnectionString = "server=" + Server + ";" + "port=" + Port + ";" +
							"user=" + ID + ";" + "password=" + PW + ";" + "database=" + Catalog + ";";
						break;
					case SQL_TYPE.POSTGRESQL:
						ConnectionString = "Host=" + Server + ";Username=" + ID + ";Password=" + PW + ";Database=" + Catalog + ";";
						break;
					case SQL_TYPE.SQLITE_ANDROID:
						break;
					case SQL_TYPE.MARIADB:
						break;
					case SQL_TYPE.ORACLE:
						break;
					}
				}
			}
			override public string ToString() {
			if (ConnectionString.Length > 0) {
				if (SqlType.Equals(SQL_TYPE.SQLITE) && !ConnectionString.ToLower().StartsWith("data source=")) {
					ConnectionString = "Data Source=" + ConnectionString;
				}
			} 
			else {
				if (SqlType.Equals (SQL_TYPE.SQLITE) && Server.Length < 1) {
					throw new Exception ("parameters not exist.");
				}
				else if (!SqlType.Equals (SQL_TYPE.SQLITE) && 
					(Server.Length < 1 || Port < 0 || ID.Length < 1 ||
						PW.Length < 1 || Catalog.Length < 1)) {
					throw new Exception ("parameters not exist.");
					//return;
				}

				switch (SqlType) {
				case SQL_TYPE.MSSQL:
					ConnectionString = "server=" + Server + ";" + (Port > 0 ? ":" + Port : "") + ";" +
						"uid=" + ID + ";" + "pwd=" + PW + ";" + "database=" + Catalog + ";";
					break;
				case SQL_TYPE.SQLITE:
					ConnectionString = "Data Source=" + Server;
					break;
				case SQL_TYPE.MYSQL:
					ConnectionString = "server=" + Server + ";" + "port=" + Port + ";" +
						"user=" + ID + ";" + "password=" + PW + ";" + "database=" + Catalog + ";";
					break;
				case SQL_TYPE.POSTGRESQL:
					ConnectionString = "Host=" + Server + ";Username=" + ID + ";Password=" + PW + ";Database=" + Catalog + ";";
					break;
				case SQL_TYPE.SQLITE_ANDROID:
					break;
				case SQL_TYPE.MARIADB:
					break;
				case SQL_TYPE.ORACLE:
					break;
				}
			}
			return ConnectionString;
		}
		/// <summary></summary>
		private string GetSqlTypeString(SQL_TYPE p_sql_type) {
			string rtn_value = "";
			switch (p_sql_type) {
				case SQL_TYPE.MSSQL: rtn_value = "mssql"; break;
				case SQL_TYPE.SQLITE: rtn_value = "sqlite"; break;
				case SQL_TYPE.MYSQL: rtn_value = "mysql"; break;
				case SQL_TYPE.POSTGRESQL: rtn_value = "postgresql"; break;
				case SQL_TYPE.MARIADB: rtn_value = "mariadb"; break;
				case SQL_TYPE.ORACLE: rtn_value = "oracle"; break;
				case SQL_TYPE.SQLITE_ANDROID: rtn_value = "sqlite_android"; break;
			}
			return rtn_value;
		}
		/// <summary></summary>
		private SQL_TYPE GetSqlType(string p_sql_type) {
			SQL_TYPE rtn_value = SQL_TYPE.MSSQL;
			switch (p_sql_type.ToLower()) {
				case "mssql": rtn_value = SQL_TYPE.MSSQL; break;
				case "sqlite": rtn_value = SQL_TYPE.SQLITE; break;
				case "mysql": rtn_value = SQL_TYPE.MYSQL; break;
				case "postgresql": rtn_value = SQL_TYPE.POSTGRESQL; break;
				case "mariadb": rtn_value = SQL_TYPE.MARIADB; break;
				case "oracle": rtn_value = SQL_TYPE.ORACLE; break;
				case "sqlite_android": rtn_value = SQL_TYPE.SQLITE_ANDROID; break;
			}
			return rtn_value;
		}
		/// <summary>DB 종류 설정</summary>
		/// <param name="sql_type"></param>
		public DBConnectionInfo SetSqlType(SQL_TYPE sql_type) {
			SqlType = sql_type;
			return this;
		}
		/// <summary>DB 연결 문자열 설정</summary>
		/// <param name="connection_string">연결 문자열</param>
		public DBConnectionInfo SetConnectionString(string connection_string) {
			ConnectionString = connection_string;
			return this;
		}
		/// <summary>DB 연결점 설정</summary>
		public DBConnectionInfo SetServer(string server) {
			Server = server;
			return this;
		}
		/// <summary>DB 연결 port 설정</summary>
		public DBConnectionInfo SetPort(int port) {
			Port = port;
			return this;
		}
		/// <summary>DB 연결시 초기에 연결할 catalog/database 설정</summary>
		public DBConnectionInfo SetCatalog(string catalog) {
			Catalog = catalog;
			return this;
		}
		/// <summary>DB 연결시 사용할 id 설정</summary>
		public DBConnectionInfo SetID(string id) {
			ID = id;
			return this;
		}
		/// <summary>DB 연결시 사용할 비번 설정</summary>
		public DBConnectionInfo SetPW(string pw) {
			PW = pw;
			return this;
		}
		// Properties below
		public SQL_TYPE SqlType { get; set; }
		public string ConnectionString { get; set; }
		public string Server { get; set; }
		public int Port { get; set; }
		public string Catalog { get; set; }
		public string ID { get; set; }
		public string PW { get; set; }
	}
	
	/// <summary>Prepared Statement 사용한 DB 처리부분</summary>
	public class Prepared {
		private AZSql azSql;
		/// <summary>기본 생성자, 새로운 AZSql 객체를 내부적으로 생성</summary>
		public Prepared() {
			azSql = new AZSql();
		}
		/// <summary>생성자, 지정된 AZSql 객체 사용</summary>
		public Prepared(AZSql azSql) {
			this.azSql = azSql;
		}
		/// <summary>생성자, 지정된 문자열을 통해 내부적으로 새로운 AZSql을 생성</summary>
		/// <param name="json">AZSql 객체 생성시 사용되는 문자열</param>
		public Prepared(string json) {
			azSql = new AZSql(json);
		}
		/// <summary>생성자, 지정된 문자열을 통해 내부적으로 가지고 있는 AZSql에 대해 Set처리</summary>
		public Prepared Set(string json) {
			azSql.Set(json);
			return this;
		}
		/// <summary>지정된 문자열을 통해 새로운 Prepared 객체 생성 후 반환</summary>
		public static Prepared Init(string json) {
			return new Prepared(json);
		}
		/// <summary>현재 Prepared 객체에 대해 실행할 쿼리 지정</summary>
		public Prepared SetQuery(string query) {
			this.azSql.SetQuery(query);
			return this;
		}
		/// <summary>현재 Prepared 객체에 대해 지정된 실행할 쿼리 반환</summary>
		public string GetQuery() {
			return this.azSql.GetQuery();
		}
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		public Prepared AddParameter(string key, object value) {
			this.azSql.AddParameter(key, value);
			return this;
		}
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">SqlDbType, value값의 DB TYPE 설정</param>
		public Prepared AddParameter(string key, object value, SqlDbType dbType) {
			this.azSql.AddParameter(key, value, dbType);
			return this;
		}
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">SqlDbType, value값의 DB TYPE 설정</param>
		/// <param name="size">int, value값의 크기 설정</param>
		public Prepared AddParameter(string key, object value, SqlDbType dbType, int size) {
			this.azSql.AddParameter(key, value, dbType, size);
			return this;
		}
#if NET_STD || NET_CORE || NET_FX || NET_STORE
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">NpgsqlTypes.NpgsqlDbType, value값의 DB TYPE 설정</param>
		public Prepared AddParameter(string key, object value, NpgsqlTypes.NpgsqlDbType dbType) {
			this.azSql.AddParameter(key, value, dbType);
			return this;
		}
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">NpgsqlTypes.NpgsqlDbType, value값의 DB TYPE 설정</param>
		/// <param name="size">int, value값의 크기 설정</param>
		public Prepared AddParameter(string key, object value, NpgsqlTypes.NpgsqlDbType dbType, int size) {
			this.azSql.AddParameter(key, value, dbType, size);
			return this;
		}
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">MySqlDbType, value값의 DB TYPE 설정</param>
		public Prepared AddParameter(string key, object value, MySqlDbType dbType) {
			this.azSql.AddParameter(key, value, dbType);
			return this;
		}
		/// <summary>parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">MySqlDbType, value값의 DB TYPE 설정</param>
		/// <param name="size">int, value값의 크기 설정</param>
		public Prepared AddParameter(string key, object value, MySqlDbType dbType, int size) {
			this.azSql.AddParameter(key, value, dbType, size);
			return this;
		}
#endif
		/// <summary>PreparedStatement 또는 StoredProcedure 사용의 경우 전달할 인수값 설정.
		/// 실제 처리는 AddParameter(string, object)의 반복</summary>
		public Prepared SetParameters(AZData parameters) {
			this.azSql.SetParameters(parameters);
			return this;
		}
		/// <summary>parameter 추가, AddParameters("key1", value1, "key2", value2...) 형식으로 사용</summary>
		/// <param name="parameters">키, 값 순서로 만들어진 object배열값</param>
		public Prepared AddParameter(params object[] parameters) {
			if (this.azSql.parameters == null) this.azSql.parameters = new AZData();
			for (int cnti=0; cnti<parameters.Length; cnti+=2) {
				this.azSql.parameters.Add(parameters[cnti].To<string>(), parameters[cnti + 1]);
			}
			return this;
		}
		/// <summary>현재 설정된 parameter 값 반환
		/// AZData(string key, ParameterData value) 형식으로 구성되어 있어, 
		/// value 값 사용시 캐스팅 필요</summary>
		public AZData GetParameters() {
			return this.azSql.GetParameters();
		}
		/// <summary>설정된 key에 해당하는 ParameterData 객체 반환</summary>
		/// <param name="key">string, Parameter 전달 key</param>
		public ParameterData GetParameter(string key) {
			return (ParameterData)this.azSql.parameters.Get(key);
		}
		/// <summary>설정된 key에 해당하는 ParameterData.Value 값 반환</summary>
		/// <param name="key">string, Parameter 전달 key</param>
		public object GetParameterValue(string key) {
			return GetParameter(key).Value;
		}
		/// <summary>설정된 key에 해당하는 ParameterData.Value 값에 대해 T형식으로 캐스팅하여 반환</summary>
		/// <param name="key">string, Parameter 전달 key</param>
		public T GetParameterValue<T>(string key) {
			return (T)GetParameter(key).Value;
		}
		/// <summary>현재 AZSql객체에 설정된 parameter 값 초기화</summary>
		public void ClearParameters() {
			this.azSql.ClearParameters();
		}
		/// <summary>현재 AZSql객체에 설정된 parameter 초기화 처리, 재사용시 새로운 객체 생성 절차가 포함됨</summary>
		public void RemoveParameters() {
			this.azSql.RemoveParameters();
		}
		/// Created in 2017-03-28, leeyonghun
		public Prepared SetReturnParameters(AZData parameters) {
			this.azSql.SetReturnParameters(parameters);
			return this;
		}
		/// <summary>현재 설정된 return parameter 값 반환
		/// AZData(string key, ParameterData value) 형식으로 구성되어 있어, 
		/// value 값 사용시 캐스팅 필요</summary>
		public AZData GetReturnParameters() {
			return this.azSql.GetReturnParameters();
		}
		/// <summary>설정된 key에 해당하는 ParameterData 객체 반환</summary>
		/// <param name="key">string, Parameter 전달 key</param>
		public ParameterData GetReturnParameter(string key) {
			return this.azSql.GetReturnParameter(key);
		}
		/// <summary>설정된 key에 해당하는 ParameterData.Value 값 반환</summary>
		/// <param name="key">string, Parameter 전달 key</param>
		public object GetReturnParameterValue(string key) {
			return GetReturnParameter(key).Value;
		}
		/// <summary>설정된 key에 해당하는 ParameterData.Value 값에 대해 T형식으로 캐스팅하여 반환</summary>
		/// <param name="key">string, Parameter 전달 key</param>
		public T GetReturnParameterValue<T>(string key) {
			return (T)GetReturnParameter(key).Value;
		}
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		public Prepared AddReturnParameter(string key, object value) {
			this.azSql.AddReturnParameter(key, value);
			return this;
		}
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">SqlDbType, value값의 DB TYPE 설정</param>
		public Prepared AddReturnParameter(string key, object value, SqlDbType dbType) {
			this.azSql.AddReturnParameter(key, value, dbType);
			return this;
		}
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">SqlDbType, value값의 DB TYPE 설정</param>
		/// <param name="size">int, value값의 크기 설정</param>
		public Prepared AddReturnParameter(string key, object value, SqlDbType dbType, int size) {
			this.azSql.AddReturnParameter(key, value, dbType, size);
			return this;
		}
#if NET_STD || NET_CORE || NET_FX || NET_STORE
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">NpgsqlTypes.NpgsqlDbType, value값의 DB TYPE 설정</param>
		public Prepared AddReturnParameter(string key, object value, NpgsqlTypes.NpgsqlDbType dbType) {
			this.azSql.AddReturnParameter(key, value, dbType);
			return this;
		}
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">NpgsqlTypes.NpgsqlDbType, value값의 DB TYPE 설정</param>
		/// <param name="size">int, value값의 크기 설정</param>
		public Prepared AddReturnParameter(string key, object value, NpgsqlTypes.NpgsqlDbType dbType, int size) {
			this.azSql.AddReturnParameter(key, value, dbType, size);
			return this;
		}
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">MySqlDbType, value값의 DB TYPE 설정</param>
		public Prepared AddReturnParameter(string key, object value, MySqlDbType dbType) {
			this.azSql.AddReturnParameter(key, value, dbType);
			return this;
		}
		/// <summary>return parameter 추가, AZData.Add(key, new ParameterData(value, dbType))로 처리됨</summary>
		/// <param name="key">string, parameter key값</param>
		/// <param name="value">object, key에 대응하는 값</param>
		/// <param name="dbType">MySqlDbType, value값의 DB TYPE 설정</param>
		/// <param name="size">int, value값의 크기 설정</param>
		public Prepared AddReturnParameter(string key, object value, MySqlDbType dbType, int size) {
			this.azSql.AddReturnParameter(key, value, dbType, size);
			return this;
		}
#endif
		/// <summary>return parameter 추가, AddReturnParameters("key1", value1, "key2", value2...) 형식으로 사용</summary>
		/// <param name="parameters">키, 값 순서로 만들어진 object배열값</param>
		public Prepared AddReturnParameters(params object[] parameters) {
			this.azSql.AddReturnParameters(parameters);
			return this;
		}
		/// <summary>return parameter 에서 idx에 해당하는 자료의 값을 설정</summary>
		/// <param name="idx">return parameter에서의 index값</param>
		/// <param name="value">해당하는 ParameterData.Value에 설정할 값</param>
		public Prepared UpdateReturnParameter(int idx, object value) {
			ParameterData data = (ParameterData)this.azSql.return_parameters.Get(idx);
			data.Value = value;
			this.azSql.return_parameters.Set(idx, data);
			return this;
		}
		/// <summary>return parameter 에서 지정된 key값에 해당하는 자료의 값을 설정</summary>
		/// <param name="key">return parameter에서의 key값</param>
		/// <param name="value">해당하는 ParameterData.Value에 설정할 값</param>
		public Prepared UpdateReturnParameter(string key, object value) {
			ParameterData data = (ParameterData)this.azSql.return_parameters.Get(key);
			data.Value = value;
			this.azSql.return_parameters.Set(key, data);
			return this;
		}
		/// <summary>현재 AZSql객체에 설정된 return_parameter 값 초기화</summary>
		public void ClearReturnParameters() {
			this.azSql.ClearReturnParameters();
		}
		/// <summary>현재 AZSql객체에 설정된 return_parameter 초기화 처리, 재사용시 새로운 객체 생성 절차가 포함됨</summary>
		public void RemoveReturnParameters() {
			this.azSql.RemoveReturnParameters();
		}
		/// <summary>insert 쿼리 실행 후 발생된 identity값을 반환 받을지 여부 설정</summary>
		public Prepared SetIdentity(bool identity) {
			this.azSql.identity = identity;
			return this;
		}
		/// <summary>insert 쿼리 실행 후 발생된 identity값을 반환 받을지 여부 반환</summary>
		public bool GetIdentity() {
			return this.azSql.identity;
		}
		/// <summary>실행할 쿼리가 stored procedure인지 여부 설정</summary>
		public Prepared SetIsStoredProcedure(bool is_stored_procedure) {
			this.azSql.SetIsStoredProcedure(is_stored_procedure);
			return this;
		}
		/// <summary>실행할 쿼리가 stored procedure인지 여부 설정값 반환</summary>
		public bool IsStoredProcedure() {
			return this.azSql.IsStoredProcedure();
		}
		/// <summary>지정된 쿼리문 실행 처리</summary>
		public int Execute() {
			return this.azSql.Execute();
		}
		/// <summary>지정된 쿼리문 실행 처리</summary>
		/// <param name="identity">쿼리 실행 후 발생되는 identity값 반환 여부 설정</param>
		public int Execute(bool identity) {
			return this.azSql.Execute(identity);
		}
		/// <summary>지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public int Execute(string query) {
			return this.azSql.Execute(query);
		}
		/// <summary>지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="identity">쿼리 실행 후 발생되는 identity값 반환 여부 설정</param>
		public int Execute(string query, bool identity) {
			return this.azSql.Execute(query, identity);
		}
		/// <summary>지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		/// <param name="identity">쿼리 실행 후 발생되는 identity값 반환 여부 설정</param>
		public int Execute(string query, AZData parameters, bool identity) {
			return this.azSql.Execute(query, parameters, identity);
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		public object Get() {
			return this.azSql.Get();
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public object Get(string query) {
			return this.azSql.Get(query);
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		public object Get(string query, AZData parameters) {
			return this.azSql.Get(query, parameters);
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		public object GetObject() {
			return this.azSql.GetObject();
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public object GetObject(string query) {
			return this.azSql.GetObject(query);
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		public object GetObject(string query, AZData parameters) {
			return this.azSql.GetObject(query, parameters);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public int GetInt() {
			return this.azSql.GetInt();
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public int GetInt(int default_value) {
			return this.azSql.GetInt(default_value);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public int GetInt(string query) {
			return this.azSql.GetInt(query);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public int GetInt(string query, int default_value) {
			return this.azSql.GetInt(query, default_value);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public int GetInt(string query, AZData parameters, int default_value) {
			return this.azSql.GetInt(query, parameters, default_value);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public long GetLong() {
			return this.azSql.GetLong();
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public long GetLong(long default_value) {
			return this.azSql.GetLong(default_value);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public long GetLong(string query) {
			return this.azSql.GetLong(query);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public long GetLong(string query, long default_value) {
			return this.azSql.GetLong(query, default_value);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public long GetLong(string query, AZData parameters, long default_value) {
			return this.azSql.GetLong(query, parameters, default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public float GetFloat() {
			return this.azSql.GetFloat();
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public float GetFloat(float default_value) {
			return this.azSql.GetFloat(default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public float GetFloat(string query) {
			return this.azSql.GetFloat(query);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public float GetFloat(string query, float default_value) {
			return this.azSql.GetFloat(query, default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public float GetFloat(string query, AZData parameters, float default_value) {
			return this.azSql.GetFloat(query, parameters, default_value);
		}
		/// <summary>단일 결과값을 string으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public string GetString() {
			return this.azSql.GetString();
		}
		/// <summary>단일 결과값을 string으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public string GetString(string query) {
			return this.azSql.GetString(query);
		}
		/// <summary>단일 결과값을 string으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public string GetString(string query, string default_value) {
			return this.azSql.GetString(query, default_value);
		}
		/// <summary>단일 결과값을 string으로 캐스팅 후 반환하는 지정된 쿼리문 실행 처리</summary>
		public string GetString(string query, AZData parameters, string default_value) {
			return this.azSql.GetString(query, parameters, default_value);
		}
		/// <summary>단일 행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		public AZData GetData() {
			return this.azSql.GetData();
		}
		/// <summary>단일 행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public AZData GetData(string query) {
			return this.azSql.GetData(query);
		}
		/// <summary>단일 행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		public AZData GetData(string query, AZData parameters) {
			return this.azSql.GetData(query, parameters);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		public AZList GetList() {
			return this.azSql.GetList();
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		public AZList GetList(int offset) {
			return this.azSql.GetList(offset);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		/// <param name="length">반환받을 행의 수 지정</param>
		public AZList GetList(int offset, int length) {
			return this.azSql.GetList(offset, length);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public AZList GetList(string query) {
			return this.azSql.GetList(query);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		public AZList GetList(string query, int offset) {
			return this.azSql.GetList(query, offset);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		public AZList GetList(string query, AZData parameters) {
			return this.azSql.GetList(query, parameters);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		public AZList GetList(string query, AZData parameters, int offset) {
			return this.azSql.GetList(query, parameters, offset);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		/// <param name="length">반환받을 행의 수 지정</param>
		public AZList GetList(string query, AZData parameters, int offset, int length) {
			return this.azSql.GetList(query, parameters, offset, length);
		}

#if NET_STD || NET_CORE || NET_FX || NET_STORE
		/// <summary>지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<int> ExecuteAsync() {
			return await this.azSql.ExecuteAsync();
		}
		/// <summary>지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="identity">쿼리 실행 후 발생되는 identity값 반환 여부 설정</param>
		public async Task<int> ExecuteAsync(bool identity) {
			return await this.azSql.ExecuteAsync(identity);
		}
		/// <summary>지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public async Task<int> ExecuteAsync(string query) {
			return await this.azSql.ExecuteAsync(query);
		}
		/// <summary>지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="identity">쿼리 실행 후 발생되는 identity값 반환 여부 설정</param>
		public async Task<int> ExecuteAsync(string query, bool identity) {
			return await this.azSql.ExecuteAsync(query, identity);
		}
		/// <summary>지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		/// <param name="identity">쿼리 실행 후 발생되는 identity값 반환 여부 설정</param>
		public async Task<int> ExecuteAsync(string query, AZData parameters, bool identity) {
			return await this.azSql.ExecuteAsync(query, parameters, identity);
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<object> GetAsync() {
			return await this.azSql.GetAsync();
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public async Task<object> GetAsync(string query) {
			return await this.azSql.GetAsync(query);
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		public async Task<object> GetAsync(string query, AZData parameters) {
			return await this.azSql.GetAsync(query, parameters);
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<object> GetObjectAsync() {
			return await this.azSql.GetObjectAsync();
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public async Task<object> GetObjectAsync(string query) {
			return await this.azSql.GetObjectAsync(query);
		}
		/// <summary>단일 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		public async Task<object> GetObjectAsync(string query, AZData parameters) {
			return await this.azSql.GetObjectAsync(query, parameters);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<int> GetIntAsync() {
			return await this.azSql.GetIntAsync();
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<int> GetIntAsync(int default_value) {
			return await this.azSql.GetIntAsync(default_value);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<int> GetIntAsync(string query) {
			return await this.azSql.GetIntAsync(query);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<int> GetIntAsync(string query, int default_value) {
			return await this.azSql.GetIntAsync(query, default_value);
		}
		/// <summary>단일 결과값을 int로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<int> GetIntAsync(string query, AZData parameters, int default_value) {
			return await this.azSql.GetIntAsync(query, parameters, default_value);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<long> GetLongAsync() {
			return await this.azSql.GetLongAsync();
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<long> GetLongAsync(int default_value) {
			return await this.azSql.GetLongAsync(default_value);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<long> GetLongAsync(string query) {
			return await this.azSql.GetLongAsync(query);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<long> GetLongAsync(string query, int default_value) {
			return await this.azSql.GetLongAsync(query, default_value);
		}
		/// <summary>단일 결과값을 long으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<long> GetLongAsync(string query, AZData parameters, int default_value) {
			return await this.azSql.GetLongAsync(query, parameters, default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<float> GetFloatAsync() {
			return await this.azSql.GetFloatAsync();
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<float> GetFloatAsync(float default_value) {
			return await this.azSql.GetFloatAsync(default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<float> GetFloatAsync(string query) {
			return await this.azSql.GetFloatAsync(query);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<float> GetFloatAsync(string query, float default_value) {
			return await this.azSql.GetFloatAsync(query, default_value);
		}
		/// <summary>단일 결과값을 float으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<float> GetFloatAsync(string query, AZData parameters, float default_value) {
			return await this.azSql.GetFloatAsync(query, parameters, default_value);
		}
		/// <summary>단일 결과값을 string으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<string> GetStringAsync() {
			return await this.azSql.GetStringAsync();
		}
		/// <summary>단일 결과값을 string으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<string> GetStringAsync(string query) {
			return await this.azSql.GetStringAsync(query);
		}
		/// <summary>단일 결과값을 string으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<string> GetStringAsync(string query, string default_value) {
			return await this.azSql.GetStringAsync(query, default_value);
		}
		/// <summary>단일 결과값을 string으로 캐스팅 후 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<string> GetStringAsync(string query, AZData parameters, string default_value) {
			return await this.azSql.GetStringAsync(query, parameters, default_value);
		}
		/// <summary>단일 행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<AZData> GetDataAsync() {
			return await this.azSql.GetDataAsync();
		}
		/// <summary>단일 행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public async Task<AZData> GetDataAsync(string query) {
			return await this.azSql.GetDataAsync(query);
		}
		/// <summary>단일 행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		public async Task<AZData> GetDataAsync(string query, AZData parameters) {
			return await this.azSql.GetDataAsync(query, parameters);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		public async Task<AZList> GetListAsync() {
			return await this.azSql.GetListAsync();
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		public async Task<AZList> GetListAsync(int offset) {
			return await this.azSql.GetListAsync(offset);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		/// <param name="length">반환받을 행의 수 지정</param>
		public async Task<AZList> GetListAsync(int offset, int length) {
			return await this.azSql.GetListAsync(offset, length);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		public async Task<AZList> GetListAsync(string query) {
			return await this.azSql.GetListAsync(query);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		public async Task<AZList> GetListAsync(string query, int offset) {
			return await this.azSql.GetListAsync(query, offset);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		public async Task<AZList> GetListAsync(string query, AZData parameters) {
			return await this.azSql.GetListAsync(query, parameters);
		}
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		public async Task<AZList> GetListAsync(string query, AZData parameters, int offset) {
			return await this.azSql.GetListAsync(query, parameters, offset);
		}
		/// <summary></summary>
		/// <summary>다행 결과값을 반환하는 지정된 쿼리문 실행 비동기 처리</summary>
		/// <param name="query">실행할 쿼리문</param>
		/// <param name="parameters">쿼리와 함께 전달항 parameter 지정</param>
		/// <param name="offset">반환받을 행의 시작점 지정</param>
		/// <param name="length">반환받을 행의 수 지정</param>
		public async Task<AZList> GetListAsync(string query, AZData parameters, int offset, int length) {
			return await this.azSql.GetListAsync(query, parameters, offset, length);
		}
#endif
	}

	public class Query {
		//private Array _select = null;
		//private Array _from = null;
		//private Array _where = null;
		
		/// <summary>접속사 정보</summary>
		public enum CONJUNCTION {
			EMPTY, AND, OR
		}

		///비교문 정보
		/// Created in 2015-06-11, leeyonghun
		public enum COMPARISON {
			EQUAL, NOT_EQUAL,
			GREATER_THAN, GREATER_THAN_OR_EQUAL,
			LESS_THAN, LESS_THAN_OR_EQUAL,
			BETWEEN,
			IN,
			LIKE
		}
		/// <summary></summary>
		public enum VALUETYPE {
			VALUE, QUERY
		}
		/// <summary></summary>
		public enum JOIN {
			EMPTY, INNER, CROSS, LEFT_OUTER, RIGHT_OUTER, FULL_OUTER
		}
		/// <summary></summary>
		public Query() {
		}
		/// <summary></summary>
		public static string MakeSelect(string p_select, int? p_count, Table p_table, Condition p_condition, Ordering p_order) {
			StringBuilder rtnValue = new StringBuilder();
			if (p_count.HasValue) {
				rtnValue.AppendFormat("SELECT TOP {0} {1}", p_count, Environment.NewLine);
			}
			else {
				rtnValue.AppendFormat("SELECT {0}", Environment.NewLine);
			}
			rtnValue.AppendFormat("	{0} {1}", p_select, Environment.NewLine);
			rtnValue.AppendFormat("FROM {0}", Environment.NewLine);
			rtnValue.AppendFormat(" {0} {1}", p_table.GetQuery(), Environment.NewLine);

			//
			if (p_condition != null && p_condition.Size() > 0) {
				rtnValue.AppendFormat("WHERE {0}", Environment.NewLine);
				//if (p_condition.GetFirstConjunction() == AZSql.Query.CONJUNCTION.EMPTY) {
				//		rtnValue.Append("	 AND ");
				//}
				rtnValue.Append(p_condition.GetQuery());
			}
			//
			if (p_order != null && p_order.Size() > 0) {
				rtnValue.AppendFormat("ORDER BY {0}", Environment.NewLine);
				rtnValue.AppendFormat(" {0}", p_order.GetQuery());
			}
			return rtnValue.ToString();
		}
			
		/// Created in 2015-08-10, leeyonghun
		public class TableData {
			public JOIN Join { get; set; }
			public string Target { get; set; }
			public Condition On { get; set; }
			/// <summary></summary>
			public TableData() {
				On = new Condition();
			}
			/// <summary></summary>
			public static TableData Init() {
				return new TableData();
			}
			/// <summary></summary>
			private string GetJoinString(JOIN p_value) {
				string rtnValue = "";
				switch (p_value) {
					case JOIN.INNER: rtnValue = " INNER JOIN "; break;
					case JOIN.LEFT_OUTER: rtnValue = " LEFT OUTER JOIN "; break;
					case JOIN.RIGHT_OUTER: rtnValue = " RIGHT OUTER JOIN "; break;
					case JOIN.FULL_OUTER: rtnValue = " FULL OUTER JOIN "; break;
					case JOIN.CROSS: rtnValue = " CROSS JOIN "; break;
				}
				return rtnValue;
			}
			/// <summary></summary>
			public TableData SetJoin(JOIN pValue) { Join = pValue; return this; }
			/// <summary></summary>
			public TableData SetTarget(string pValue) { Target = pValue; return this; }
			/// <summary></summary>
			public TableData SetOn(Condition pValue) { On = pValue; return this; }
			/// <summary></summary>
			public TableData AddOn(ConditionData pValue) {
				if (this.On == null) {
					this.On = new Condition();
				}
				this.On.Add(pValue);
				return this; 
			}
			/// <summary></summary>
			public string ToJsonString() {
				StringBuilder rtnValue = new StringBuilder();
				rtnValue.Append("{");
				if (Join != JOIN.EMPTY) {
					rtnValue.AppendFormat("\"{0}\": \"{1}\"", "join", GetJoinString(Join));
					rtnValue.AppendFormat(",\"{0}\": \"{1}\"", "target", Target);
				}
				else {
					rtnValue.AppendFormat("\"{0}\": \"{1}\"", "target", Target);
				}
				rtnValue.AppendFormat(",\"{0}\": {1}", "on", On.ToJsonString());
				rtnValue.Append("}");
				return rtnValue.ToString();
			}
			/// <summary></summary>
			public TableData Clear() {
				Join = JOIN.EMPTY;
				Target = "";
				On.Clear();

				return this;
			}
		}

		public class Table {
			List<TableData> tableList;
			/// <summary></summary>
			public Table() {
				tableList = new List<TableData>();
			}
			/// <summary></summary>
			public static Table Init() {
				return new Table();
			}
			/// <summary></summary>
			public Table Add(TableData p_value) {
				this.tableList.Add(p_value);
				return this;
			}
			/// <summary></summary>
			public TableData Get(int p_value) {
				return this.tableList[p_value];
			}
			/// <summary></summary>
			public string ToJsonString() {
				StringBuilder rtnValue = new StringBuilder();
				rtnValue.Append("[");
				for (int cnti = 0; cnti < this.tableList.Count; cnti++) {
					rtnValue.AppendFormat("{0}{1}", cnti > 0 ? "," : "", this.tableList[cnti].ToJsonString());
				}
				rtnValue.Append("]");
				return rtnValue.ToString();
			}
			/// <summary></summary>
			public JOIN GetFirstJoin() {
				JOIN rtnValue = JOIN.EMPTY;
				if (Size() > 0) rtnValue = tableList[0].Join;
				return rtnValue;
			}
			/// <summary></summary>
			public int Size() {
				return this.tableList.Count;
			}
			/// <summary></summary>
			public string GetQuery() {
				return AZSql.Query.Table.GetQuery(ToJsonString());
			}
			/// <summary></summary>
			public static string GetQuery(string p_json) {
				StringBuilder rtn_value = new StringBuilder();

				AZList list = AZString.JSON.Init(p_json).ToAZList();
				AZData query = new AZData();
				AZData group = new AZData();		// {"group_name":[]}
				for (int cnti = 0; cnti < list.Size(); cnti++) {
					AZData data = list.Get(cnti);
					string data_join = data.GetString("join");
					string data_target = data.GetString("target");
					AZList list_on = data.GetList("on");

					rtn_value.AppendFormat("		{0} {1} {2}", data_join, data_target, Environment.NewLine);
					if (list_on.Size() > 0) {
						rtn_value.AppendFormat("				{0} ({1}{2}				) {3}", "on", Environment.NewLine, AZSql.Query.Condition.GetQuery(list_on.ToJsonString()).Replace("	 ", "						"), Environment.NewLine);
					}
				}

				return rtn_value.ToString();
			}
			/// <summary></summary>
			public Table Clear() {
				this.tableList.Clear();
				return this;
			}
		}

		/// <summary></summary>
		/// Created in 2015-08-04, leeyonghun
		public class OrderingData {
			public int Order { get; set; }
			public string Value { get; set; }
			/// <summary></summary>
			public OrderingData() {}
			/// <summary></summary>
			public OrderingData(int pOrder, string pValue) {
				Set(pOrder, pValue);
			}
			/// <summary></summary>
			public static OrderingData Init(int pOrder, string pValue) {
				return new OrderingData(pOrder, pValue);
			}
			/// <summary></summary>
			public OrderingData Set(int pOrder, string pValue) {
				Order = pOrder;
				Value = pValue;
				return this;
			}
			/// <summary></summary>
			public string ToJsonString() {
				StringBuilder rtnValue = new StringBuilder();
				rtnValue.Append("{");
				rtnValue.AppendFormat("\"order\": \"{0}\"", Order);
				rtnValue.AppendFormat(",\"value\": \"{0}\"", Value);
				rtnValue.Append("}");
				return rtnValue.ToString();
			}
		}

		public class Ordering {
			List<OrderingData> orderingList;
			/// <summary></summary>
			public Ordering() {
				orderingList = new List<OrderingData>();
			}
			/// <summary></summary>
			public static Ordering Init() {
				return new Ordering();
			}
			/// <summary></summary>
			public Ordering Add(OrderingData p_value) {
				this.orderingList.Add(p_value);
				return this;
			}
			/// <summary></summary>
			public Ordering Add(int p_order, string p_value) {
				this.Add(new OrderingData(p_order, p_value));
				return this;
			}
			/// <summary></summary>
			public string ToJsonString() {
				StringBuilder rtnValue = new StringBuilder();
				rtnValue.Append("[");
				for (int cnti = 0; cnti < this.orderingList.Count; cnti++) {
					rtnValue.AppendFormat("{0}{1}", cnti > 0 ? "," : "", this.orderingList[cnti].ToJsonString());
				}
				rtnValue.Append("]");
				return rtnValue.ToString();
			}
			/// <summary></summary>
			public string GetQuery() {
				return AZSql.Query.Ordering.GetQuery(ToJsonString());
			}
			/// <summary></summary>
			public int Size() {
				return this.orderingList.Count;
			}
			/// <summary>[{"order":"1~", "value":""},,,]</summary>
			public static string GetQuery(string p_json) {
				StringBuilder rtn_value = new StringBuilder();

				List<AZData> list_order = new List<AZData>();
				AZList list = AZString.JSON.Init(p_json).ToAZList();
				for (int cnti = 0; cnti < list.Size(); cnti++) {
					AZData data = list.Get(cnti);
					int data_order = data.GetInt("order", 0);
					string data_value = data.GetString("value");

					bool is_inserted = false;
					for (int cntk = 0; cntk < list_order.Count; cntk++) {
						if (list_order[cntk].GetInt("order", -1) > data_order) {
							list_order.Insert(cntk, data);
							is_inserted = true;
							break;
						}
					}
					if (!is_inserted) {
						list_order.Add(data);
					}
				}
				list = null;
				for (int cnti = 0; cnti < list_order.Count; cnti++) {
					AZData data = list_order[cnti];
					rtn_value.Append((cnti > 0 ? ", " : "") + data.GetString("value"));
				}
				return rtn_value.ToString();
			}
			/// <summary></summary>
			public Ordering Clear() {
				this.orderingList.Clear();
				return this;
			}
    }

    /// <summary></summary>
    public class ConditionData {
      public string Group { get; set; }
      public CONJUNCTION Conjunction { get; set; }
      public string Target { get; set; }
      public COMPARISON Comparison { get; set; }
      public List<string> Values { get; set; }

      /// Created in 2015-08-04, leeyonghun
      public ConditionData() {
        Group = "";
        Conjunction = CONJUNCTION.EMPTY;
        Comparison = COMPARISON.EQUAL;
        Values = new List<string>();
      }
      /// Created in 2015-08-04, leeyonghun
      public static ConditionData Init() {
        return new ConditionData();
      }
      /// Created in 2015-08-04, leeyonghun
      private string GetComparisonString(COMPARISON p_value) {
        string rtnValue = "";
        switch (p_value) {
          case COMPARISON.EQUAL: rtnValue = "="; break;
          case COMPARISON.NOT_EQUAL: rtnValue = "<>"; break;
          case COMPARISON.GREATER_THAN: rtnValue = ">"; break;
          case COMPARISON.GREATER_THAN_OR_EQUAL: rtnValue = ">="; break;
          case COMPARISON.LESS_THAN: rtnValue = "<"; break;
          case COMPARISON.LESS_THAN_OR_EQUAL: rtnValue = "<="; break;
          case COMPARISON.BETWEEN: rtnValue = "BETWEEN"; break;
          case COMPARISON.IN: rtnValue = "IN"; break;
          case COMPARISON.LIKE: rtnValue = "LIKE"; break;
        }
        return rtnValue;
      }
      /// <summary></summary>
      public ConditionData SetGroup(string pValue) { Group = pValue; return this; }
      /// <summary></summary>
      public ConditionData SetConjunction(CONJUNCTION pValue) { Conjunction = pValue; return this; }
      /// <summary></summary>
      public ConditionData SetTarget(string pValue) { Target = pValue; return this; }
      /// <summary></summary>
      public ConditionData SetComparison(COMPARISON pValue) { Comparison = pValue; return this; }
      /// <summary></summary>
      public ConditionData SetValues(List<string> pValue) { Values = Values; return this; }
      /// <summary></summary>
      public ConditionData SetValue(int pValue) { Values.Clear(); return AddValue("" + pValue); }
      /// <summary></summary>
      public ConditionData SetValue(float pValue) { Values.Clear(); return AddValue("" + pValue); }
      /// <summary></summary>
      public ConditionData SetValue(string pValue) { Values.Clear(); return AddValue(pValue); }
      /// <summary></summary>
      public ConditionData AddValue(int pValue) { AddValue("" + pValue, VALUETYPE.VALUE); return this; }
      /// <summary></summary>
      public ConditionData AddValue(float pValue) { AddValue("" + pValue, VALUETYPE.VALUE); return this; }
      /// <summary></summary>
      public ConditionData AddValue(string pValue) { AddValue(pValue, VALUETYPE.VALUE); return this; }
      /// <summary></summary>
      public ConditionData AddValue(string pValue, VALUETYPE pValueType) { Values.Add(pValueType.Equals(VALUETYPE.VALUE) ? "'" + pValue + "'" : pValue); return this; }
      /// <summary></summary>
      public string ToJsonString() {
        StringBuilder rtnValue = new StringBuilder();
        rtnValue.Append("{");
        rtnValue.AppendFormat("\"{0}\": \"{1}\"", "group", Group);
        if (Conjunction == CONJUNCTION.EMPTY) {
          rtnValue.AppendFormat(",\"{0}\": \"\"", "conjunction");
        }
        else {
          rtnValue.AppendFormat(",\"{0}\": \"{1}\"", "conjunction", Conjunction);
        }
        rtnValue.AppendFormat(",\"{0}\": \"{1}\"", "target", Target);
        rtnValue.AppendFormat(",\"{0}\": \"{1}\"", "comparison", GetComparisonString(Comparison));

        string values = "[";
        for (int cnti = 0; cnti < Values.Count; cnti++) {
          values += ((cnti > 0 ? "," : "") + "{\"value\": \"" + Values[cnti] + "\"}");
        }
        values += "]";

        rtnValue.AppendFormat(",\"{0}\": {1}", "values", values);
        rtnValue.Append("}");
        return rtnValue.ToString();
      }
      /// <summary></summary>
      public ConditionData Clear() {
        Group = "";
        Conjunction = CONJUNCTION.EMPTY;
        Target = "";
        Comparison = COMPARISON.EQUAL;
        Values.Clear();

        return this;
      }
    }

    public class Condition {
      // [{"order":"1~", "group":"", "conjunction":"and|or...", "target":"", "comparison":"=|<>|...", "values":[{"value":""},,,]},,,]
      List<ConditionData> conditionalList;
      /// <summary></summary>
      public Condition() {
        conditionalList = new List<ConditionData>();
      }
      /// <summary></summary>
      public static Condition Init() {
        return new Condition();
      }
      /// <summary></summary>
      public Condition Add(ConditionData p_value) {
        this.conditionalList.Add(p_value);
        return this;
      }
      /// <summary></summary>
      public string ToJsonString() {
        StringBuilder rtnValue = new StringBuilder();
        rtnValue.Append("[");
        for (int cnti = 0; cnti < this.conditionalList.Count; cnti++) {
          rtnValue.AppendFormat("{0}{1}", cnti > 0 ? "," : "", this.conditionalList[cnti].ToJsonString());
        }
        rtnValue.Append("]");
        return rtnValue.ToString();
      }
      /// <summary></summary>
      public CONJUNCTION GetFirstConjunction() {
        CONJUNCTION rtnValue = CONJUNCTION.EMPTY;
        if (Size() > 0) {
          rtnValue = conditionalList[0].Conjunction;
        }
        return rtnValue;
      }
      /// <summary></summary>
      public int Size() {
        return this.conditionalList.Count;
      }
      /// <summary></summary>
      public string GetQuery() {
        return AZSql.Query.Condition.GetQuery(ToJsonString());
      }
      /// <summary></summary>
      public static string GetQuery(string p_json) {
        StringBuilder rtn_value = new StringBuilder();

        AZList list = AZString.JSON.Init(p_json).ToAZList();
        AZData query = new AZData();
        AZData group = new AZData();		// {"group_name":[]}
        for (int cnti = 0; cnti < list.Size(); cnti++) {
          AZData data = list.Get(cnti);
          //int data_order = data.GetInt("order");
          string data_group = data.GetString("group");
          string data_conjunction = data.GetString("conjunction");
          string data_target = data.GetString("target");
          string data_comparison = data.GetString("comparison");
          AZList data_values = data.GetList("values");

          // group 값을 결정짓고
          data_group = data_group.Trim().Length < 1 ? "_____" : data_group;

          // 해당 데이터에서 group 값을 삭제
          data.Remove("group");

          if (group.HasKey(data_group)) {
            // 해당 키값에 대한 자료가 존재하는 경우 -> 기존 자료에 목록 추가
            AZList dummy = group.GetList(data_group);
            dummy.Add(data);
            group.Set(data_group, dummy);
          }
          else {
            // 해당 키값에 대한 자료가 존재하지 않는 경우 -> 새로운 자료 추가
            AZList dummy = new AZList();
            dummy.Add(data);
            group.Add(data_group, dummy);
          }
        }

        for (int cnti = 0; cnti < group.Size(); cnti++) {
          AZList group_list = group.GetList(cnti);
          string group_key = group.GetKey(cnti);
          string tab_string = "	 ";

          // 그룹 내 목록이 1개 초과인 경우
          if (group_list.Size() > 1) {
            if (group_key.Equals("_____")) {
              rtn_value.Append(tab_string + group_list.Get(0).GetString("conjunction") + " ");
            }
            else {
              rtn_value.Append(tab_string + group_list.Get(0).GetString("conjunction") + " (" + Environment.NewLine);
              tab_string = "			";
            }

            for (int cntk = 0; cntk < group_list.Size(); cntk++) {
              //rtn_value.Append(tab_string);
              if (cntk > 0) {
                rtn_value.Append(tab_string);
                rtn_value.Append(group_list.Get(cntk).GetString("conjunction") + " ");
              }
              string sub_target = group_list.Get(cntk).GetString("target");
              string sub_comparison = group_list.Get(cntk).GetString("comparison");
              rtn_value.Append(sub_target + (sub_comparison.Length > 0 ? " " + sub_comparison : sub_comparison) + " ");
              switch (sub_comparison.ToLower()) {
                case "between":
                  if (group_list.Get(cntk).GetList("values").Size() > 1) {
                    rtn_value.Append(group_list.Get(cntk).GetList("values").Get(0).GetString("value") + " AND " + group_list.Get(cntk).GetList("values").Get(1).GetString("value") + Environment.NewLine);
                  }
                  else if (group_list.Get(cntk).GetList("values").Size() == 1) {
                    rtn_value.Append(group_list.Get(cntk).GetList("values").Get(0).GetString("value") + Environment.NewLine);
                  }
                  break;
                case "in":
                  rtn_value.Append("(");
                  for (int cntm = 0; cntm < group_list.Get(cntk).GetList("values").Size(); cntm++) {
                    if (cntm > 0) {
                      rtn_value.Append(", ");
                    }
                    rtn_value.AppendFormat("{0}", group_list.Get(cntk).GetList("values").Get(cntm).GetString("value"));
                  }
                  rtn_value.AppendFormat(") {0}", Environment.NewLine);
                  break;
                default:
                  rtn_value.Append(group_list.Get(cntk).GetList("values").Get(0).GetString("value") + Environment.NewLine);
                  break;
              }
            }

            if (group_key.Equals("_____")) {
            }
            else {
                rtn_value.Append("	)" + Environment.NewLine);
            }
          }
          else {
            rtn_value.Append("	" + group_list.Get(0).GetString("conjunction") + " ");
            string sub_target = group_list.Get(0).GetString("target");
            string sub_comparison = group_list.Get(0).GetString("comparison");
            rtn_value.Append(sub_target + " " + sub_comparison + " ");
            switch (sub_comparison.ToLower()) {
              case "between":
                if (group_list.Get(0).GetList("values").Size() > 1) {
                  rtn_value.Append(group_list.Get(0).GetList("values").Get(0).GetString("value") + " AND " + group_list.Get(0).GetList("values").Get(1).GetString("value") + Environment.NewLine);
                }
                else if (group_list.Get(0).GetList("values").Size() == 1) {
                  rtn_value.Append(group_list.Get(0).GetList("values").Get(0).GetString("value") + Environment.NewLine);
                }
                break;
              case "in":
                rtn_value.Append("(");
                for (int cntm = 0; cntm < group_list.Get(0).GetList("values").Size(); cntm++) {
                  if (cntm > 0) {
                    rtn_value.Append(", ");
                  }
                  rtn_value.AppendFormat("{0}", group_list.Get(0).GetList("values").Get(cntm).GetString("value"));
                }
                rtn_value.AppendFormat(") {0}", Environment.NewLine);
                break;
              default:
                rtn_value.Append(group_list.Get(0).GetList("values").Get(0).GetString("value") + Environment.NewLine);
                break;
            }
          }
        }
        return rtn_value.ToString();
      }
      /// <summary></summary>
      public Condition Clear() {
        this.conditionalList.Clear();
        return this;
      }
    }
  }

public class BQuery {
	public enum WHERETYPE {
		GREATER_THAN, GREATER_THAN_OR_EQUAL, GT, GTE, 
		LESS_THAN, LESS_THAN_OR_EQUAL, LT, LTE, 
		EQUAL, NOT_EQUAL, EQ, NE, 
		BETWEEN, 
		IN, NOT_IN, NIN, 
		LIKE
	}
	public enum VALUETYPE { VALUE, QUERY }
	public enum CREATE_QUERY_TYPE { INSERT, UPDATE, DELETE, SELECT }
	protected class ATTRIBUTE {
		public const string VALUE = "value";
		public const string WHERE = "where";
	}
	public class SetList {
		List<SetData> setList;
		/// <summary>기본생성자</summary>
		public SetList() {
			setList = new List<SetData>();
		}
		/// <summary>인스턴스 생성 후 인스턴스 반환</summary>
		public static SetList Init() {
			return new SetList();
		}
		/// <summary></summary>
		public SetList Add(SetData p_value) {
			this.setList.Add(p_value);
			return this;
		}
		/// <summary></summary>
		public SetList Add(string p_column, string p_value, VALUETYPE p_value_type) {
			return Add(new SetData(p_column, p_value, p_value_type));
		}
		/// <summary></summary>
		public SetList Add(string p_column, string p_value) {
			return Add(new SetData(p_column, p_value));
		}
		/// <summary></summary>
		public SetData Get(int p_index) {
			return this.setList[p_index];
		}
		/// <summary></summary>
		public SetData this[int p_index] {
			get { return Get(p_index); }
		}
		/// <summary></summary>
		public string GetQuery() {
			StringBuilder rtn_value = new StringBuilder();
			for (int cnti = 0; cnti < this.setList.Count; cnti++) {
				if (cnti < 1) {
					rtn_value.AppendFormat("		 {0} {1}", this.setList[cnti].GetQuery(), Environment.NewLine);
				}
				else {
					rtn_value.AppendFormat("		,{0} {1}", this.setList[cnti].GetQuery(), Environment.NewLine);
				}
			}
			return rtn_value.ToString();
		}
		/// <summary></summary>
		public int Size() {
			return this.setList.Count;
		}
	}

	/// <summary></summary>
	public class SetData {
		public string Column { get; set; }
		public string Value { get; set; }
		public VALUETYPE ValueType { get; set; }

		/// <summary>기본생성자</summary>
		public SetData() {
			ValueType = VALUETYPE.VALUE;
		}
		/// <summary>인스턴스 생성 후 인스턴스 반환 처리</summary>
		public static SetData Init() {
			return new SetData();
		}
		/// <summary>생성자</summary>
		public SetData(string p_column, string p_value) {
			Set(p_column, p_value, VALUETYPE.VALUE);
		}
		/// <summary>생성자</summary>
		public SetData(string p_column, string p_value, VALUETYPE p_value_type) {
			Set(p_column, p_value, p_value_type);
		}				
		/// <summary>기본값 설정</summary>
		public void Set(string p_column, string p_value, VALUETYPE p_value_type) {
			this.Column = p_column;
			this.Value = p_value;
			this.ValueType = p_value_type;
		}
		/// <summary></summary>
		public string GetQuery() {
			string rtn_value = "";
			switch (this.ValueType) {
				case VALUETYPE.QUERY:
					rtn_value = this.Column + " = " + this.Value;
					break;
				case VALUETYPE.VALUE:
					rtn_value = this.Column + " = '" + this.Value + "'";
					break;
			}
			return rtn_value;
		}
	}

  public class Condition {
    public string Column {get;set;}
    public object Value {get;set;}
    public object[] Values {get;set;}
    public Nullable<WHERETYPE> WhereType {get;set;}
    public Nullable<VALUETYPE> ValueType {get;set;}
    private bool Prepared {get;set;}
    /// <summary>기본 생성자</summary>
    public Condition() {
      WhereType = WHERETYPE.EQUAL;
      ValueType = VALUETYPE.VALUE;
      Prepared = false;
    }
    /// <summary></summary>
    /// <param name="column"></param>
    /// <param name="value"></param>
    public Condition(string column, object value) {
      WhereType = WHERETYPE.EQUAL;
      ValueType = VALUETYPE.VALUE;
      Prepared = false;
      Set(column, value, null, null);
    }
    /// <summary></summary>
    /// <param name="column"></param>
    /// <param name="value"></param>
    /// <param name="whereType"></param>
    public Condition(string column, object value, WHERETYPE whereType) {
      WhereType = WHERETYPE.EQUAL;
      ValueType = VALUETYPE.VALUE;
      Prepared = false;
      Set(column, value, whereType, null);
    }
    /// <summary></summary>
    /// <param name="column"></param>
    /// <param name="value"></param>
    /// <param name="whereType"></param>
    /// <param name="valueType"></param>
    public Condition(string column, object value, WHERETYPE whereType, VALUETYPE valueType) {
      WhereType = WHERETYPE.EQUAL;
      ValueType = VALUETYPE.VALUE;
      Prepared = false;
      Set(column, value, whereType, valueType);
    }
    /// <summary></summary>
    /// <param name="column"></param>
    /// <param name="value"></param>
    /// <param name="whereType"></param>
    /// <param name="valueType"></param>
    public void Set(string column, object value, Nullable<WHERETYPE> whereType, Nullable<VALUETYPE> valueType) {
      this.Column = column;
      this.Value = value;
      this.Values = null;
      if (whereType.HasValue) this.WhereType = whereType.Value;
      if (valueType.HasValue) this.ValueType = valueType.Value;
    }
    /// <summary></summary>
    /// <param name="column"></param>
    /// <param name="values"></param>
    public Condition(string column, object[] values) {
      Set(column, values, null, null);
    }
    /// <summary></summary>
    /// <param name="column"></param>
    /// <param name="values"></param>
    /// <param name="whereType"></param>
    public Condition(string column, object[] values, WHERETYPE whereType) {
      Set(column, values, whereType, null);
    }
    /// <summary></summary>
    /// <param name="column"></param>
    /// <param name="values"></param>
    /// <param name="whereType"></param>
    /// <param name="valueType"></param>
    public Condition(string column, object[] values, WHERETYPE whereType, VALUETYPE valueType) {
      Set(column, values, whereType, valueType);
    }
    /// <summary></summary>
    /// <param name="column"></param>
    /// <param name="values"></param>
    /// <param name="whereType"></param>
    /// <param name="valueType"></param>
    public void Set(string column, object[] values, Nullable<WHERETYPE> whereType, Nullable<VALUETYPE> valueType) {
      this.Column = column;
      this.Value = null;
      this.Values = values;
      if (whereType.HasValue) this.WhereType = whereType.Value;
      if (valueType.HasValue) this.ValueType = valueType.Value;
    }
    public Condition SetPrepared(bool prepared) {
      Prepared = prepared;
      return this;
    }
    public bool IsPrepared() {
      return Prepared;
    }
    /// <summary></summary>
    public AZData ToAZData(ref int index) {
      index++;
      AZData rtnValue = new AZData();
      if (IsPrepared()) {
        switch (ValueType.Value) {
          case VALUETYPE.VALUE:
            switch (WhereType.Value) {
              case WHERETYPE.EQUAL: case WHERETYPE.EQ: 
              case WHERETYPE.GREATER_THAN: case WHERETYPE.GT: 
              case WHERETYPE.GREATER_THAN_OR_EQUAL: case WHERETYPE.GTE: 
              case WHERETYPE.LESS_THAN: case WHERETYPE.LT: 
              case WHERETYPE.LESS_THAN_OR_EQUAL: case WHERETYPE.LTE: 
              case WHERETYPE.NOT_EQUAL: case WHERETYPE.NE: 
              case WHERETYPE.LIKE: 
                rtnValue.Add(
                  string.Format("@{0}_where_{1}", Column.Replace(".", "___"), index), 
                  Value
                );
                break;
              case WHERETYPE.BETWEEN:
                rtnValue.Add(
                  string.Format("@{0}_where_{1}_between_1", Column.Replace(".", "___"), index), 
                  Values[0]
                );
                rtnValue.Add(
                  string.Format("@{0}_where_{1}_between_2", Column.Replace(".", "___"), index), 
                  Values[1]
                );
                break;
              case WHERETYPE.IN: 
              case WHERETYPE.NOT_IN: case WHERETYPE.NIN:
                for (int cnti=0; cnti<Values.Length; cnti++) {
                  rtnValue.Add(
                    string.Format("@{0}_where_{1}_in_{2}", Column.Replace(".", "___"), index, cnti + 1), 
                    Values[cnti]
                  );
                }
                break;
            }
            break;
        }
      }
      return rtnValue;
    }
    /// <summary></summary>
    public string ToString(ref int index) {
      index++;
      int passIndex = index;
      StringBuilder rtnValue = new StringBuilder();
      switch (ValueType.Value) {
        case VALUETYPE.QUERY:
          switch (WhereType.Value) {
            case WHERETYPE.EQUAL: case WHERETYPE.EQ:
              rtnValue.AppendFormat("{0} {1} {2}", Column, "=", Value); break;
            case WHERETYPE.GREATER_THAN: case WHERETYPE.GT:
              rtnValue.AppendFormat("{0} {1} {2}", Column, ">", Value); break;
            case WHERETYPE.GREATER_THAN_OR_EQUAL: case WHERETYPE.GTE:
              rtnValue.AppendFormat("{0} {1} {2}", Column, ">=", Value); break;
            case WHERETYPE.LESS_THAN: case WHERETYPE.LT:
              rtnValue.AppendFormat("{0} {1} {2}", Column, "<", Value); break;
            case WHERETYPE.LESS_THAN_OR_EQUAL: case WHERETYPE.LTE:
              rtnValue.AppendFormat("{0} {1} {2}", Column, "<=", Value); break;
            case WHERETYPE.NOT_EQUAL: case WHERETYPE.NE:
              rtnValue.AppendFormat("{0} {1} {2}", Column, "<>", Value); break;
            case WHERETYPE.LIKE:
              rtnValue.AppendFormat("{0} {1} {2}", Column, "LIKE", Value); break;
            case WHERETYPE.BETWEEN:
              rtnValue.AppendFormat("{0} {1} {2} AND {3}", Column, "BETWEEN", Values[0], Values[1]); break;
            case WHERETYPE.IN: 
              rtnValue.AppendFormat("{0} {1} ({2})", Column, "IN", Values.Join(", ")); break;
            case WHERETYPE.NOT_IN: case WHERETYPE.NIN:
              rtnValue.AppendFormat("{0} {1} {2} ({3})", "NOT", Column, "IN", Values.Join(", ")); break;
          }
          break;
        case VALUETYPE.VALUE:
          string valStr = null;
          switch (WhereType.Value) {
            case WHERETYPE.EQUAL: case WHERETYPE.EQ: 
            case WHERETYPE.GREATER_THAN: case WHERETYPE.GT: 
            case WHERETYPE.GREATER_THAN_OR_EQUAL: case WHERETYPE.GTE: 
            case WHERETYPE.LESS_THAN: case WHERETYPE.LT: 
            case WHERETYPE.LESS_THAN_OR_EQUAL: case WHERETYPE.LTE: 
            case WHERETYPE.NOT_EQUAL: case WHERETYPE.NE: 
            case WHERETYPE.LIKE: 
              valStr = IsPrepared() 
                ? string.Format("@{0}_where_{1}", Column.Replace(".", "___"), index)
                : string.Format("'{0}'", Value);
              break;
          }
          switch (WhereType.Value) {
            case WHERETYPE.EQUAL: case WHERETYPE.EQ:
              rtnValue.AppendFormat("{0} {1} {2}", Column, "=", valStr); break;
            case WHERETYPE.GREATER_THAN: case WHERETYPE.GT:
              rtnValue.AppendFormat("{0} {1} {2}", Column, ">", valStr); break;
            case WHERETYPE.GREATER_THAN_OR_EQUAL: case WHERETYPE.GTE:
              rtnValue.AppendFormat("{0} {1} {2}", Column, ">=", valStr); break;
            case WHERETYPE.LESS_THAN: case WHERETYPE.LT:
              rtnValue.AppendFormat("{0} {1} {2}", Column, "<", valStr); break;
            case WHERETYPE.LESS_THAN_OR_EQUAL: case WHERETYPE.LTE:
              rtnValue.AppendFormat("{0} {1} {2}", Column, "<=", valStr); break;
            case WHERETYPE.NOT_EQUAL: case WHERETYPE.NE:
              rtnValue.AppendFormat("{0} {1} {2}", Column, "<>", valStr); break;
            case WHERETYPE.LIKE:
              rtnValue.AppendFormat("{0} {1} {2}", Column, "LIKE", valStr); break;
            case WHERETYPE.BETWEEN:
              rtnValue.AppendFormat("{0} {1} {2} AND {3}", Column, "BETWEEN", 
                IsPrepared() 
                  ? string.Format("@{0}_where_{1}_between_1", Column.Replace(".", "___"), index)
                  : string.Format("'{0}'", Values[0]),
                IsPrepared() 
                  ? string.Format("@{0}_where_{1}_between_2", Column.Replace(".", "___"), index)
                  : string.Format("'{0}'", Values[1])
              );
              break;
            case WHERETYPE.IN: 
              int subInIdx = 1;
              rtnValue.AppendFormat("{0} {1} ({2})", Column, "IN", 
                IsPrepared()
                  ? Values.Each(x => string.Format("@{0}_where_{1}_in_{2}", Column.Replace(".", "___"), passIndex, subInIdx++)).Join(", ")
                  : Values.Each(x => AZString.Init(x).String("").Wrap("'")).Join(", ")
              );
              break;
            case WHERETYPE.NOT_IN: case WHERETYPE.NIN:
              int subNinIdx = 1;
              rtnValue.AppendFormat("{0} {1} {2} ({3})", "NOT", Column, "IN", 
                IsPrepared()
                  ? Values.Each(x => string.Format("@{0}_where_{1}_in_{2}", Column.Replace(".", "___"), passIndex, subNinIdx++)).Join(", ")
                  : Values.Each(x => AZString.Init(x).String("").Wrap("'")).Join(", ")
              );
              break;
          }
          break;
      }
      return rtnValue.ToString();
    }

#if NET_STD || NET_CORE || NET_FX || NET_STORE
    /// <summary></summary>
    public BsonDocument ToBsonDocument() {
      BsonDocument rtnValue = new BsonDocument();
      switch (ValueType.Value) {
        case VALUETYPE.VALUE:
	        string compStr = "";
	        switch (WhereType.Value) {
		        case WHERETYPE.EQUAL: case WHERETYPE.EQ: compStr = "$eq"; break;
		        case WHERETYPE.GREATER_THAN: case WHERETYPE.GT: compStr = "$gt"; break;
		        case WHERETYPE.GREATER_THAN_OR_EQUAL: case WHERETYPE.GTE: compStr = "$gte"; break;
		        case WHERETYPE.LESS_THAN: case WHERETYPE.LT: compStr = "$lt"; break;
		        case WHERETYPE.LESS_THAN_OR_EQUAL: case WHERETYPE.LTE: compStr = "$lte"; break;
		        case WHERETYPE.NOT_EQUAL: case WHERETYPE.NE: compStr = "$ne"; break;
	        }

	        switch (WhereType.Value) {
            case WHERETYPE.EQUAL: case WHERETYPE.EQ:
            case WHERETYPE.GREATER_THAN: case WHERETYPE.GT:
            case WHERETYPE.GREATER_THAN_OR_EQUAL: case WHERETYPE.GTE:
            case WHERETYPE.LESS_THAN_OR_EQUAL: case WHERETYPE.LTE:
            case WHERETYPE.LESS_THAN: case WHERETYPE.LT:
            case WHERETYPE.NOT_EQUAL: case WHERETYPE.NE:
	            if (Value.GetType().Equals(typeof(Int16))) {
		            rtnValue.Add(Column, new BsonDocument(compStr, (short)Value));
	            }
	            else if (Value.GetType().Equals(typeof(Int32))) {
		            rtnValue.Add(Column, new BsonDocument(compStr, (int)Value));
	            }
	            else if (Value.GetType().Equals(typeof(Int64))) {
		            rtnValue.Add(Column, new BsonDocument(compStr, (long)Value));
	            }
	            else {
		            rtnValue.Add(Column, new BsonDocument(compStr, AZString.Init(Value).String()));
	            }
	            break;
            case WHERETYPE.IN:
              BsonArray inArrs = new BsonArray();
              //int inArrIdx = 0;
	            //Values.Each(x => { inArrs.Add(Values[inArrIdx++]); });
	            foreach (var value in Values) {
		            if (value.GetType().Equals(typeof(string))) {
			            inArrs.Add((string)value);
		            }
		            else if (value.GetType().Equals(typeof(Int16))) {
			            inArrs.Add((short)value);
		            }
		            else if (value.GetType().Equals(typeof(Int32))) {
			            inArrs.Add((int)value);
		            }
		            else if (value.GetType().Equals(typeof(Int64))) {
			            inArrs.Add((long)value);
		            }
	            }
              rtnValue = new BsonDocument(Column, new BsonDocument("$in", inArrs)); break;
            case WHERETYPE.NOT_IN: case WHERETYPE.NIN: 
              BsonArray ninArrs = new BsonArray();
              //int ninArrIdx = 0;
              //Values.Each(x => { ninArrs.Add(Values[ninArrIdx++]); });
	            foreach (var value in Values) {
		            if (value.GetType().Equals(typeof(string))) {
			            ninArrs.Add((string)value);
		            }
		            else if (value.GetType().Equals(typeof(Int16))) {
			            ninArrs.Add((short)value);
		            }
		            else if (value.GetType().Equals(typeof(Int32))) {
			            ninArrs.Add((int)value);
		            }
		            else if (value.GetType().Equals(typeof(Int64))) {
			            ninArrs.Add((long)value);
		            }
	            }
              rtnValue = new BsonDocument(Column, new BsonDocument("$nin", ninArrs)); break;
          }
          break;
      }
      return rtnValue;
    }
#endif
  }

  /// <summary></summary>
  public class And {
    private ArrayList ands;
    private bool Prepared {get;set;}
    public And(params object[] conditions) {
      ands = new ArrayList();
      ands.AddRange(conditions);
    }
    /// <summary></summary>
    public And Add(params object[] conditions) {
      ands.AddRange(conditions);
      return this;
    }
    /// <summary></summary>
    public And Add(object condition) {
      ands.Add(condition);
      return this;
    }
    public And SetPrepared(bool prepared) {
      Prepared = prepared;
      return this;
    }
    public bool IsPrepared() {
      return Prepared;
    }
    /// <summary></summary>
    public int Count() {
      return ands.Count;
    }
    /// <summary></summary>
    public AZData ToAZData(ref int index) {
      AZData rtnValue = new AZData();
      if (IsPrepared()) {
        //int conditionIdx = Index;
        foreach (object data in ands) {
          if (data.GetType().Equals(typeof(And))) {
            rtnValue.Add(((And)data).SetPrepared(IsPrepared()).ToAZData(ref index));
            //conditionIdx += ((And)data).Count();
          }
          else if (data.GetType().Equals(typeof(Or))) {
            rtnValue.Add(((Or)data).SetPrepared(IsPrepared()).ToAZData(ref index));
            //conditionIdx += ((Or)data).Count();
          }
          else if (data.GetType().Equals(typeof(Condition))) {
            rtnValue.Add(((Condition)data).SetPrepared(IsPrepared()).ToAZData(ref index));
            //conditionIdx++;
          }
        }
      }
      return rtnValue;
    }
    /// <summary></summary>
    public string ToString(ref int index) {
      StringBuilder rtnValue = new StringBuilder();
      rtnValue.Append("(");
      int idx = 0;
      //int conditionIdx = Index;
      foreach (object data in ands) {
        rtnValue.AppendFormat("{0}", idx == 0 ? "" : "AND ");
        if (data.GetType().Equals(typeof(And))) {
          rtnValue.Append(((And)data).SetPrepared(IsPrepared()).ToString(ref index));
          //conditionIdx += ((And)data).Count();
        }
        else if (data.GetType().Equals(typeof(Or))) {
          rtnValue.Append(((Or)data).SetPrepared(IsPrepared()).ToString(ref index));
          //conditionIdx += ((Or)data).Count();
        }
        else if (data.GetType().Equals(typeof(Condition))) {
          rtnValue.Append(((Condition)data).SetPrepared(IsPrepared()).ToString(ref index));
          //conditionIdx++;
        }
        idx++;
        if (idx < ands.Count) rtnValue.Append(Environment.NewLine);
      }
      rtnValue.Append(")");
      return rtnValue.ToString();
    }

#if NET_STD || NET_CORE || NET_FX || NET_STORE
    /// <summary></summary>
    public BsonDocument ToBsonDocument() {
      BsonArray arr = new BsonArray();
      foreach (object data in ands) {
        if (data.GetType().Equals(typeof(And))) {
          arr.Add(((And)data).ToBsonDocument());
        }
        else if (data.GetType().Equals(typeof(Or))) {
          arr.Add(((Or)data).ToBsonDocument());
        }
        else if (data.GetType().Equals(typeof(Condition))) {
          arr.Add(((Condition)data).ToBsonDocument());
        }
      }
      return new BsonDocument("$and", arr);
    }
#endif
  }

  /// <summary></summary>
  public class Or {
    private ArrayList ors;
    private bool Prepared {get;set;}
    public Or(params object[] conditions) {
      ors = new ArrayList();
      ors.AddRange(conditions);
    }
    /// <summary></summary>
    public Or Add(params object[] conditions) {
      ors.AddRange(conditions);
      return this;
    }
    /// <summary></summary>
    public Or Add(object condition) {
      ors.Add(condition);
      return this;
    }
    public Or SetPrepared(bool prepared) {
      Prepared = prepared;
      return this;
    }
    public bool IsPrepared() {
      return Prepared;
    }
    /// <summary></summary>
    public int Count() {
      return ors.Count;
    }
    /// <summary></summary>
    public AZData ToAZData(ref int index) {
      AZData rtnValue = new AZData();
      if (IsPrepared()) {
        //int conditionIdx = Index;
        foreach (object data in ors) {
          if (data.GetType().Equals(typeof(And))) {
            rtnValue.Add(((And)data).SetPrepared(IsPrepared()).ToAZData(ref index));
            //conditionIdx += ((And)data).Count();
          }
          else if (data.GetType().Equals(typeof(Or))) {
            rtnValue.Add(((Or)data).SetPrepared(IsPrepared()).ToAZData(ref index));
            //conditionIdx += ((Or)data).Count();
          }
          else if (data.GetType().Equals(typeof(Condition))) {
            rtnValue.Add(((Condition)data).SetPrepared(IsPrepared()).ToAZData(ref index));
            //conditionIdx++;
          }
        }
      }
      return rtnValue;
    }
    /// <summary></summary>
    public string ToString(ref int index) {
      StringBuilder rtnValue = new StringBuilder();
      rtnValue.Append("(");
      int idx = 0;
      //int conditionIdx = Index;
      foreach (object data in ors) {
        rtnValue.AppendFormat("{0}", idx == 0 ? "" : "OR ");
        if (data.GetType().Equals(typeof(And))) {
          rtnValue.Append(((And)data).SetPrepared(IsPrepared()).ToString(ref index));
          //conditionIdx += ((And)data).Count();
        }
        else if (data.GetType().Equals(typeof(Or))) {
          rtnValue.Append(((Or)data).SetPrepared(IsPrepared()).ToString(ref index));
          //conditionIdx += ((Or)data).Count();
        }
        else if (data.GetType().Equals(typeof(Condition))) {
          rtnValue.Append(((Condition)data).SetPrepared(IsPrepared()).ToString(ref index));
          //conditionIdx++;
        }
        idx++;
        if (idx < ors.Count) rtnValue.Append(Environment.NewLine);
      }
      rtnValue.Append(")");
      return rtnValue.ToString();
    }

#if NET_STD || NET_CORE || NET_FX || NET_STORE
    /// <summary></summary>
    public BsonDocument ToBsonDocument() {
      BsonArray arr = new BsonArray();
      foreach (object data in ors) {
        if (data.GetType().Equals(typeof(And))) {
          arr.Add(((And)data).ToBsonDocument());
        }
        else if (data.GetType().Equals(typeof(Or))) {
          arr.Add(((Or)data).ToBsonDocument());
        }
        else if (data.GetType().Equals(typeof(Condition))) {
          arr.Add(((Condition)data).ToBsonDocument());
        }
      }
      return new BsonDocument("$or", arr);
    }
#endif
  }

	protected string table_name;
	//private DBConnectionInfo db_info;
	//private AZList sql_where, sql_set;
	protected AZList sql_set;
  protected ArrayList sql_where;
	//private string query;
	protected string sql_select;
	protected bool prepared {get;set;}
	/// <summary>Prepared Statement 사용 여부 반환</summary>
	public bool IsPrepared() {
		return this.prepared;
	}
	/// <summary>Prepared Statement 사용 여부 설정</summary>
	public BQuery SetIsPrepared(bool value) {
		this.prepared = value;
		return this;
	}

	/// <summary>기본 생성자</summary>
	public BQuery () {
    sql_where = new ArrayList();
		sql_set = new AZList();
		sql_select = "";
    //
    SetIsPrepared(false);
	}

	/// <summary>생성자</summary>
	public BQuery (string table_name) {
		if (table_name.Trim().Length < 1) {
			throw new Exception("Target table name not specified.");
		}
		this.table_name = AZString.Encode(AZString.ENCODE.JSON, table_name);
		//this.db_info = new DBConnectionInfo(connection_json);

		//sql_where = new AZList();
    sql_where = new ArrayList();
		sql_set = new AZList();
		sql_select = "";
    //
    SetIsPrepared(false);
	}
	/// <summary>생성자</summary>
	public BQuery (string table_name, bool prepared) {
		if (table_name.Trim().Length < 1) {
			throw new Exception("Target table name not specified.");
		}
		this.table_name = AZString.Encode(AZString.ENCODE.JSON, table_name);
		//this.db_info = new DBConnectionInfo(connection_json);

		//sql_where = new AZList();
    sql_where = new ArrayList();
		sql_set = new AZList();
		sql_select = "";
		//data_schema = null;

		//has_schema_data = false;
		//
		SetIsPrepared(prepared);
	}
	/// <summary>Creating new class and return</summary>
	public static AZSql.BQuery Init(string p_table_name) {
		if (p_table_name.Trim().Length < 1) {
			throw new Exception("Target table name not specified.");
		}
		return new AZSql.BQuery(p_table_name);
	}
	/// <summary>Creating new class and return</summary>
	public static AZSql.BQuery Init(string p_table_name, bool prepared) {
		if (p_table_name.Trim().Length < 1) {
			throw new Exception("Target table name not specified.");
		}
		return new AZSql.BQuery(p_table_name, prepared);
	}
	/// <summary></summary>
	public void Clear() {
		this.sql_set.Clear();
		this.sql_where.Clear();
		this.sql_select = "";
	}
	/// <summary></summary>
	public AZSql.BQuery Select(string value) {
		this.sql_select = value;
		return this;
	}
	/// <summary></summary>
	public AZSql.BQuery Set(SetData p_set_data) {
		if (p_set_data != null) {
			Set(p_set_data.Column, p_set_data.Value, p_set_data.ValueType);
		}
		return this;
	}
	/// <summary></summary>
	public AZSql.BQuery Set(SetData[] p_set_datas) {
		if (p_set_datas != null) {
			for (int cnti = 0; cnti < p_set_datas.Length; cnti++) {
				if (p_set_datas[cnti] != null) {
					Set(p_set_datas[cnti]);
				}
			}
		}
		return this;
	}
	/// <summary></summary>
	public AZSql.BQuery Set(SetList p_set_list) {
		if (p_set_list != null) {
			for (int cnti = 0; cnti < p_set_list.Size(); cnti++) {
				if (p_set_list.Get(cnti) != null) {
					Set(p_set_list.Get(cnti));
				}
			}
		}
		return this;
	}
	/// <summary></summary>
	public AZSql.BQuery Set(string p_column, object p_value) {
		return Set(p_column, p_value, VALUETYPE.VALUE);
	}
	/// <summary></summary>
	public AZSql.BQuery Set(string p_column, object p_value, VALUETYPE p_valuetype) {
		if (p_column.Trim().Length < 1) {
			throw new Exception("Target column name is not specified.");
		}
		/*
    if (HasSchemaData()) {
			if (!this.data_schema.HasKey(p_column)) {
				throw new Exception("Target column name is not exist.");
			}
		}
    */
		AZData data = new AZData();
		data.Attribute.Add(ATTRIBUTE.VALUE, p_valuetype);
		data.Add(p_column, p_value);

		this.sql_set.Add(data);
		data = null;

		return this;
	}
	/// <summary></summary>
	public AZSql.BQuery ClearSet() {
		this.sql_set.Clear();
		return this;
	}
  /// <summary></summary>
  public AZSql.BQuery Where(Or conditions) {
		this.sql_where.Add(conditions);
    return this;
  }
  /// <summary></summary>
  public AZSql.BQuery Where(And conditions) {
		this.sql_where.Add(conditions);
    return this;
  }
  /// <summary></summary>
  public AZSql.BQuery Where(Condition condition) {
		if (condition.Column.Trim().Length < 1) {
			throw new Exception("Target column name is not specified.");
		}
    /*
		if (HasSchemaData() && !this.data_schema.HasKey(condition.Column)) {
			throw new Exception("Target column name is not exist.");
		}
    */
		this.sql_where.Add(condition);
		return this;
  }
	/// <summary></summary>
	public AZSql.BQuery Where(string p_column, object p_value) {
    return Where(new Condition(p_column, p_value, WHERETYPE.EQUAL, VALUETYPE.VALUE));
		//return Where(p_column, p_value, WHERETYPE.EQUAL, VALUETYPE.VALUE);
	}
	/// <summary></summary>
	public AZSql.BQuery Where(string p_column, object p_value, WHERETYPE p_wheretype) {
    return Where(new Condition(p_column, p_value, p_wheretype, VALUETYPE.VALUE));
		//return Where(p_column, p_value, p_wheretype, VALUETYPE.VALUE);
	}
	/// <summary></summary>
	public AZSql.BQuery Where(string p_column, object p_value, WHERETYPE p_wheretype, VALUETYPE p_valuetype) {
		if (p_column.Trim().Length < 1) {
			throw new Exception("Target column name is not specified.");
		}
    /*
		if (HasSchemaData()) {
			if (!this.data_schema.HasKey(p_column)) {
				throw new Exception("Target column name is not exist.");
			}
		}
    */
    return Where(new Condition(p_column, p_value, p_wheretype, p_valuetype));
		/*AZData data = new AZData();
		data.Attribute.Add(ATTRIBUTE.WHERE, p_wheretype);
		data.Attribute.Add(ATTRIBUTE.VALUE, p_valuetype);
		data.Add(p_column, p_value);

		this.sql_where.Add(data);
		data = null;

		return this;*/
	}
	/// <summary></summary>
	public AZSql.BQuery Where(string p_column, object[] p_value) {
		return Where(p_column, p_value, WHERETYPE.EQUAL, VALUETYPE.VALUE);
	}
	/// <summary></summary>
	public AZSql.BQuery Where(string p_column, object[] p_value, WHERETYPE p_wheretype) {
		return Where(p_column, p_value, p_wheretype, VALUETYPE.VALUE);
	}
	/// <summary></summary>
	public AZSql.BQuery Where(string p_column, object[] p_values, WHERETYPE p_wheretype, VALUETYPE p_valuetype) {
		if (p_column.Trim().Length < 1) {
			throw new Exception("Target column name is not specified.");
		}
    /*
		if (HasSchemaData()) {
			if (!this.data_schema.HasKey(p_column)) {
				throw new Exception("Target column name is not exist.");
			}
		}
    */
		AZData data = new AZData();
		data.Attribute.Add(ATTRIBUTE.WHERE, p_wheretype);
		data.Attribute.Add(ATTRIBUTE.VALUE, p_valuetype);
		for (int cnti = 0; cnti < p_values.Length; cnti++) {
			data.Add(p_column, p_values[cnti]);
		}

		this.sql_where.Add(data);
		data = null;

		return this;
	}
	/// <summary></summary>
	public AZSql.BQuery ClearWhere() {
		this.sql_where.Clear();
		return this;
	}
	/// <summary>특정된 쿼리 타입에 맞게 현재의 자료를 바탕으로 쿼리 문자열 생성</summary>
	private string CreateQuery(CREATE_QUERY_TYPE p_type) {
		StringBuilder rtn_value = new StringBuilder();
    int idx = 0;
		switch (p_type) {
			case CREATE_QUERY_TYPE.SELECT:
				rtn_value.AppendFormat("SELECT{0}", Environment.NewLine);
				rtn_value.AppendFormat(" {0}{1}", this.sql_select, Environment.NewLine);
				rtn_value.AppendFormat("FROM{0}", Environment.NewLine);
				rtn_value.AppendFormat(" {0}{1}", this.table_name, Environment.NewLine);
        //
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
					//if (cnti == 0) rtn_value.AppendFormat("WHERE{0}", Environment.NewLine);
          rtn_value.AppendFormat("{0}", cnti == 0 ? "WHERE\r\n" : "AND ");
					//AZData data = this.sql_where.Get(cnti);
          object row = this.sql_where[cnti];
          if (row.GetType().Equals(typeof(Condition))) {
            Condition data = (Condition)row;
            rtn_value.Append(data.SetPrepared(IsPrepared()).ToString(ref idx));
            //idx++;
          }
          else if (row.GetType().Equals(typeof(And))) {
            And data = (And)row;
            rtn_value.Append(data.SetPrepared(IsPrepared()).ToString(ref idx));
            //idx += data.Count();
          }
          else if (row.GetType().Equals(typeof(Or))) {
            Or data = (Or)row;
            rtn_value.Append(data.SetPrepared(IsPrepared()).ToString(ref idx));
            //idx += data.Count();
          }
          rtn_value.Append(Environment.NewLine);
          /*
          switch (data.Attribute.Get(ATTRIBUTE.VALUE)) {
            case VALUETYPE.QUERY:
              break;
          }
					if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.QUERY)) {
						rtn_value.Append("	" + (cnti > 0 ? " AND " : "") + data.GetKey(0));

						if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.EQUAL)) {
							rtn_value.Append(" = ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.GREATER_THAN)) {
							rtn_value.Append(" > ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.GREATER_THAN_OR_EQUAL)) {
							rtn_value.Append(" >= ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.LESS_THAN)) {
							rtn_value.Append(" < ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.LESS_THAN_OR_EQUAL)) {
							rtn_value.Append(" <= ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.NOT_EQUAL)) {
							rtn_value.Append(" <> ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.LIKE)) {
							rtn_value.Append(" LIKE ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.BETWEEN)) {
							rtn_value.Append(" BETWEEN ");
							rtn_value.Append(data.GetString(0));
							if (data.Size() > 1) {
								rtn_value.Append(" AND " + data.GetString(1));
							}
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.IN)) {
							rtn_value.Append(" IN ( ");
							for (int cntk = 0; cntk < data.Size(); cntk++) {
								rtn_value.Append((cntk > 0 ? ", " : "") + data.GetString(cntk));
							}
							rtn_value.Append(" ) ");
						}
						rtn_value.Append(Environment.NewLine);
					}
					else if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.VALUE)) {
						rtn_value.Append("	" + (cnti > 0 ? " AND " : "") + data.GetKey(0));

						switch (data.Attribute.Get(ATTRIBUTE.WHERE)) {
							case WHERETYPE.EQUAL: rtn_value.Append(" = "); break;
							case WHERETYPE.GREATER_THAN: rtn_value.Append(" > "); break;
							case WHERETYPE.GREATER_THAN_OR_EQUAL: rtn_value.Append(" >= "); break;
							case WHERETYPE.LESS_THAN: rtn_value.Append(" < "); break;
							case WHERETYPE.LESS_THAN_OR_EQUAL: rtn_value.Append(" <= "); break;
							case WHERETYPE.NOT_EQUAL: rtn_value.Append(" <> "); break;
							case WHERETYPE.LIKE: rtn_value.Append(" LIKE "); break;
							case WHERETYPE.BETWEEN:
								rtn_value.Append(" BETWEEN ");
								if (!IsPrepared()) {
									rtn_value.Append("'" + data.GetString(0) + "'");
									if (data.Size() > 1) {
										rtn_value.Append(" AND " + "'" + data.GetString(0) + "'");
									}
								}
								else {
									rtn_value.Append("@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1) + "_between_1");
									if (data.Size() > 1) {
										rtn_value.Append(" AND " + "@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1) + "_between_2");
									}
								}
								break;
							case WHERETYPE.IN:
								rtn_value.Append(" IN ( ");
								if (!IsPrepared()) {
									for (int cntk = 0; cntk < data.Size(); cntk++) {
										rtn_value.Append((cntk > 0 ? ", " : "") + "'" + data.GetString(0) + "'");
									}
								}
								else {
									for (int cntk = 0; cntk < data.Size(); cntk++) {
										rtn_value.Append((cntk > 0 ? ", " : "") + "@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1) + "_in_" + (cntk + 1));
									}
								}
								rtn_value.Append(" ) ");
								break;
						}
						switch (data.Attribute.Get(ATTRIBUTE.WHERE)) {
							case WHERETYPE.EQUAL:
							case WHERETYPE.GREATER_THAN:
							case WHERETYPE.GREATER_THAN_OR_EQUAL:
							case WHERETYPE.LESS_THAN:
							case WHERETYPE.LESS_THAN_OR_EQUAL:
							case WHERETYPE.NOT_EQUAL:
							case WHERETYPE.LIKE:
								if (!IsPrepared()) {
									rtn_value.Append("'" + data.GetString(0) + "'");
								}
								else {
									rtn_value.Append("@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1));
								}
								break;
						}
						rtn_value.Append(Environment.NewLine);
					}
          */
				}
				break;
			case CREATE_QUERY_TYPE.INSERT:
				//rtn_value.Append("INSERT INTO " + table_name + " ( " + Environment.NewLine);
        rtn_value.AppendFormat("INSERT INTO {0} ({1}", table_name, Environment.NewLine);
				for (int cnti = 0; cnti < this.sql_set.Size(); cnti++) {
					AZData data = this.sql_set.Get(cnti);
					//rtn_value.Append("	" + (cnti > 0 ? ", " : "") + data.GetKey(0));
          rtn_value.AppendFormat(" {0}{1}{2}", cnti == 0 ? "" : ",", data.GetKey(0), Environment.NewLine);
				}
				rtn_value.Append(Environment.NewLine + ") " + Environment.NewLine);
				//rtn_value.Append("VALUES ( " + Environment.NewLine);
        rtn_value.AppendFormat("VALUES ({0}", Environment.NewLine);
				for (int cnti = 0; cnti < this.sql_set.Size(); cnti++) {
					AZData data = this.sql_set.Get(cnti);
          rtn_value.Append(cnti == 0 ? " " : " ,");
					if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.QUERY)) {
						//rtn_value.Append("	" + (cnti > 0 ? ", " : "") + data.GetString(0) + Environment.NewLine);
            rtn_value.AppendFormat("{0}{1}", data.GetString(0), Environment.NewLine);
					}
					else if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.VALUE)) {
						/*if (!IsPrepared()) {
							rtn_value.Append("	" + (cnti > 0 ? ", " : "") + "'" + data.GetString(0) + "'" + Environment.NewLine);
						}
						else {
							rtn_value.Append("	" + (cnti > 0 ? ", " : "") + "@" + data.GetKey(0).Replace(".", "___") + "_set_" + (cnti + 1) + Environment.NewLine);
						}*/
            rtn_value.AppendFormat("{0}{1}",
              IsPrepared()
								? string.Format("@{0}_set_{1}", data.GetKey(0).Replace(".", "___"), cnti + 1)
								: string.Format("'{0}'", data.GetString(0)),
              Environment.NewLine
            );
					}
				}
				rtn_value.Append(")");
				break;
			case CREATE_QUERY_TYPE.UPDATE:
				//rtn_value.Append("UPDATE " + table_name + " " + Environment.NewLine);
				//rtn_value.Append("SET " + Environment.NewLine);
        rtn_value.AppendFormat("UPDATE {0}{1}", table_name, Environment.NewLine);
        rtn_value.AppendFormat("SET{0}", Environment.NewLine);
				for (int cnti = 0; cnti < this.sql_set.Size(); cnti++) {
					AZData data = this.sql_set.Get(cnti);
          rtn_value.Append(cnti > 0 ? " ," : " ");
					if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.QUERY)) {
						//rtn_value.Append("	" + (cnti > 0 ? ", " : "") + data.GetKey(0) + " = " + data.GetString(0) + Environment.NewLine);
            rtn_value.AppendFormat(" {0}={1}{2}", data.GetKey(0), data.GetString(0), Environment.NewLine);
					}
					else if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.VALUE)) {
						/*if (!IsPrepared()) {
							rtn_value.Append("	" + (cnti > 0 ? ", " : "") + data.GetKey(0) + " = " + "'" + data.GetString(0) + "'" + Environment.NewLine);
						}
						else {
							rtn_value.Append("	" + (cnti > 0 ? ", " : "") + data.GetKey(0) + " = " + "@" + data.GetKey(0).Replace(".", "___") + "_set_" + (cnti + 1) + Environment.NewLine);
						}*/
            rtn_value.AppendFormat(" {0}={1}{2}", data.GetKey(0), 
              IsPrepared() 
                ? string.Format("@{0}_set_{1}", data.GetKey(0).Replace(".", "___"), (cnti + 1))
                : string.Format("'{0}", data.GetString(0)), 
              Environment.NewLine
            );
					}
				}
        //
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
          rtn_value.AppendFormat("{0}", cnti == 0 ? "WHERE\r\n" : "AND ");
          object row = this.sql_where[cnti];
          if (row.GetType().Equals(typeof(Condition))) {
            Condition data = (Condition)row;
            rtn_value.Append(data.SetPrepared(IsPrepared()).ToString(ref idx));
            //idx++;
          }
          else if (row.GetType().Equals(typeof(And))) {
            And data = (And)row;
            rtn_value.Append(data.SetPrepared(IsPrepared()).ToString(ref idx));
            //idx += data.Count();
          }
          else if (row.GetType().Equals(typeof(Or))) {
            Or data = (Or)row;
            rtn_value.Append(data.SetPrepared(IsPrepared()).ToString(ref idx));
            //idx += data.Count();
          }
          rtn_value.Append(Environment.NewLine);
          /*
					AZData data = this.sql_where.Get(cnti);
					if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.QUERY)) {
						rtn_value.Append("	" + (cnti > 0 ? " AND " : "") + data.GetKey(0));

						if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.EQUAL)) {
							rtn_value.Append(" = ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.GREATER_THAN)) {
							rtn_value.Append(" > ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.GREATER_THAN_OR_EQUAL)) {
							rtn_value.Append(" >= ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.LESS_THAN)) {
							rtn_value.Append(" < ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.LESS_THAN_OR_EQUAL)) {
							rtn_value.Append(" <= ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.NOT_EQUAL)) {
							rtn_value.Append(" <> ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.LIKE)) {
							rtn_value.Append(" LIKE ");
							rtn_value.Append(data.GetString(0));
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.BETWEEN)) {
							rtn_value.Append(" BETWEEN ");
							rtn_value.Append(data.GetString(0));
							if (data.Size() > 1) {
								rtn_value.Append(" AND " + data.GetString(1));
							}
						}
						else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.IN)) {
							rtn_value.Append(" IN ( ");
							for (int cntk = 0; cntk < data.Size(); cntk++) {
								rtn_value.Append((cntk > 0 ? ", " : "") + data.GetString(cntk));
							}
							rtn_value.Append(" ) ");
						}
						rtn_value.Append(Environment.NewLine);
					}
					else if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.VALUE)) {
						rtn_value.Append("	" + (cnti > 0 ? " AND " : "") + data.GetKey(0));

						switch (data.Attribute.Get(ATTRIBUTE.WHERE)) {
							case WHERETYPE.EQUAL: rtn_value.Append(" = "); break;
							case WHERETYPE.GREATER_THAN: rtn_value.Append(" > "); break;
							case WHERETYPE.GREATER_THAN_OR_EQUAL: rtn_value.Append(" >= "); break;
							case WHERETYPE.LESS_THAN: rtn_value.Append(" < "); break;
							case WHERETYPE.LESS_THAN_OR_EQUAL: rtn_value.Append(" <= "); break;
							case WHERETYPE.NOT_EQUAL: rtn_value.Append(" <> "); break;
							case WHERETYPE.LIKE: rtn_value.Append(" LIKE "); break;
							case WHERETYPE.BETWEEN:
								rtn_value.Append(" BETWEEN ");
								if (!IsPrepared()) {
									rtn_value.Append("'" + data.GetString(0) + "'");
									if (data.Size() > 1) {
										rtn_value.Append(" AND " + "'" + data.GetString(0) + "'");
									}
								}
								else {
									rtn_value.Append("@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1) + "_between_1");
									if (data.Size() > 1) {
										rtn_value.Append(" AND " + "@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1) + "_between_2");
									}
								}
								break;
							case WHERETYPE.IN:
								rtn_value.Append(" IN ( ");
								if (!IsPrepared()) {
									for (int cntk = 0; cntk < data.Size(); cntk++) {
										rtn_value.Append((cntk > 0 ? ", " : "") + "'" + data.GetString(0) + "'");
									}
								}
								else {
									for (int cntk = 0; cntk < data.Size(); cntk++) {
										rtn_value.Append((cntk > 0 ? ", " : "") + "@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1) + "_in_" + (cntk + 1));
									}
								}
								rtn_value.Append(" ) ");
								break;
							}
							switch (data.Attribute.Get(ATTRIBUTE.WHERE)) {
								case WHERETYPE.EQUAL:
								case WHERETYPE.GREATER_THAN:
								case WHERETYPE.GREATER_THAN_OR_EQUAL:
								case WHERETYPE.LESS_THAN:
								case WHERETYPE.LESS_THAN_OR_EQUAL:
								case WHERETYPE.NOT_EQUAL:
								case WHERETYPE.LIKE:
									if (!IsPrepared()) {
										rtn_value.Append("'" + data.GetString(0) + "'");
									}
									else {
										rtn_value.Append("@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1));
									}
									break;
							}
							rtn_value.Append(Environment.NewLine);
						}
            */
					}
					break;
				case CREATE_QUERY_TYPE.DELETE:
					//rtn_value.Append("DELETE FROM " + table_name + " " + Environment.NewLine);
          rtn_value.AppendFormat("DELETE FROM {0}{1}", table_name, Environment.NewLine);
          //
					for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
            rtn_value.AppendFormat("{0}", cnti == 0 ? "WHERE\r\n" : "AND ");
            object row = this.sql_where[cnti];
            if (row.GetType().Equals(typeof(Condition))) {
              Condition data = (Condition)row;
              rtn_value.Append(data.SetPrepared(IsPrepared()).ToString(ref idx));
              //idx++;
            }
            else if (row.GetType().Equals(typeof(And))) {
              And data = (And)row;
              rtn_value.Append(data.SetPrepared(IsPrepared()).ToString(ref idx));
              //idx += data.Count();
            }
            else if (row.GetType().Equals(typeof(Or))) {
              Or data = (Or)row;
              rtn_value.Append(data.SetPrepared(IsPrepared()).ToString(ref idx));
              //idx += data.Count();
            }
            rtn_value.Append(Environment.NewLine);
            /*
						AZData data = this.sql_where.Get(cnti);
						if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.QUERY)) {
							rtn_value.Append("	" + (cnti > 0 ? " AND " : "") + data.GetKey(0));

							if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.EQUAL)) {
								rtn_value.Append(" = ");
								rtn_value.Append(data.GetString(0));
							}
							else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.GREATER_THAN)) {
								rtn_value.Append(" > ");
								rtn_value.Append(data.GetString(0));
							}
							else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.GREATER_THAN_OR_EQUAL)) {
								rtn_value.Append(" >= ");
								rtn_value.Append(data.GetString(0));
							}
							else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.LESS_THAN)) {
								rtn_value.Append(" < ");
								rtn_value.Append(data.GetString(0));
							}
							else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.LESS_THAN_OR_EQUAL)) {
								rtn_value.Append(" <= ");
								rtn_value.Append(data.GetString(0));
							}
							else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.NOT_EQUAL)) {
								rtn_value.Append(" <> ");
								rtn_value.Append(data.GetString(0));
							}
							else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.LIKE)) {
								rtn_value.Append(" LIKE ");
								rtn_value.Append(data.GetString(0));
							}
							else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.BETWEEN)) {
								rtn_value.Append(" BETWEEN ");
								rtn_value.Append(data.GetString(0));
								if (data.Size() > 1) {
									rtn_value.Append(" AND " + data.GetString(1));
								}
							}
							else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.IN)) {
								rtn_value.Append(" IN ( ");
								for (int cntk = 0; cntk < data.Size(); cntk++) {
									rtn_value.Append((cntk > 0 ? ", " : "") + data.GetString(cntk));
								}
								rtn_value.Append(" ) ");
							}
							rtn_value.Append(Environment.NewLine);
						}
						else if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.VALUE)) {
							rtn_value.Append("	" + (cnti > 0 ? " AND " : "") + data.GetKey(0));

							switch (data.Attribute.Get(ATTRIBUTE.WHERE)) {
								case WHERETYPE.EQUAL: rtn_value.Append(" = "); break;
								case WHERETYPE.GREATER_THAN: rtn_value.Append(" > "); break;
								case WHERETYPE.GREATER_THAN_OR_EQUAL: rtn_value.Append(" >= "); break;
								case WHERETYPE.LESS_THAN: rtn_value.Append(" < "); break;
								case WHERETYPE.LESS_THAN_OR_EQUAL: rtn_value.Append(" <= "); break;
								case WHERETYPE.NOT_EQUAL: rtn_value.Append(" <> "); break;
								case WHERETYPE.LIKE: rtn_value.Append(" LIKE "); break;
								case WHERETYPE.BETWEEN:
									rtn_value.Append(" BETWEEN ");
									if (!IsPrepared()) {
										rtn_value.Append("'" + data.GetString(0) + "'");
										if (data.Size() > 1) {
											rtn_value.Append(" AND " + "'" + data.GetString(0) + "'");
										}
									}
									else {
										rtn_value.Append("@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1) + "_between_1");
										if (data.Size() > 1) {
											rtn_value.Append(" AND " + "@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1) + "_between_2");
										}
									}
									break;
								case WHERETYPE.IN:
									rtn_value.Append(" IN ( ");
									if (!IsPrepared()) {
										for (int cntk = 0; cntk < data.Size(); cntk++) {
											rtn_value.Append((cntk > 0 ? ", " : "") + "'" + data.GetString(0) + "'");
										}
									}
									else {
										for (int cntk = 0; cntk < data.Size(); cntk++) {
											rtn_value.Append((cntk > 0 ? ", " : "") + "@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1) + "_in_" + (cntk + 1));
										}
									}
									rtn_value.Append(" ) ");
									break;
							}
							switch (data.Attribute.Get(ATTRIBUTE.WHERE)) {
								case WHERETYPE.EQUAL:
								case WHERETYPE.GREATER_THAN:
								case WHERETYPE.GREATER_THAN_OR_EQUAL:
								case WHERETYPE.LESS_THAN:
								case WHERETYPE.LESS_THAN_OR_EQUAL:
								case WHERETYPE.NOT_EQUAL:
								case WHERETYPE.LIKE:
									if (!IsPrepared()) {
										rtn_value.Append("'" + data.GetString(0) + "'");
									}
									else {
										rtn_value.Append("@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1));
									}
									break;
							}
							rtn_value.Append(Environment.NewLine);
						}
            */
					}
					break;
				}
				return rtn_value.ToString();
			}
			/// <summary>특정된 쿼리 실행 종류에 맞는 쿼리 문자열 생성 후 반환</summary>
			public string GetQuery(CREATE_QUERY_TYPE p_create_query_type) {
				return CreateQuery(p_create_query_type);
			}
			/// <summary>Prepared Statement 용 전달 인수 객체를 반환한다</summary>
			public AZData GetPreparedParameters() {
				AZData rtn_value = new AZData();
				for (int cnti = 0; cnti < this.sql_set.Size(); cnti++) {
					AZData data = this.sql_set.Get(cnti);
					if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.VALUE)) {
						if (rtn_value == null) rtn_value = new AZData();
						rtn_value.Add("@" + data.GetKey(0).Replace(".", "___") + "_set_" + (cnti + 1), data.Get(0));
					}
				}
        //
        int idx = 0;
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
          object row = this.sql_where[cnti];
          if (row == null) continue;
          if (row.GetType().Equals(typeof(Condition))) {
            Condition data = (Condition)row;
            rtn_value.Add(data.SetPrepared(IsPrepared()).ToAZData(ref idx));
            //idx++;
          }
          else if (row.GetType().Equals(typeof(And))) {
            And data = (And)row;
            rtn_value.Add(data.SetPrepared(IsPrepared()).ToAZData(ref idx));
            //idx += data.Count();
          }
          else if (row.GetType().Equals(typeof(Or))) {
            Or data = (Or)row;
            rtn_value.Add(data.SetPrepared(IsPrepared()).ToAZData(ref idx));
            //idx += data.Count();
          }
					/*
          AZData data = this.sql_where.Get(cnti);
					if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.VALUE)) {
						if (rtn_value == null) rtn_value = new AZData();
						switch (data.Attribute.Get(ATTRIBUTE.WHERE)) {
							case WHERETYPE.IN:
								for (int cntk = 0; cntk < data.Size(); cntk++) {
									rtn_value.Add("@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1) + "_in_" + (cntk + 1), data.Get(cntk));
								}
								break;
							case WHERETYPE.BETWEEN:
								for (int cntk = 0; cntk < data.Size(); cntk++) {
									if (cntk > 1) break;
									rtn_value.Add("@" + data.GetKey(0).Replace(".", "___") + "_between_" + (cnti + 1) + "_in_" + (cntk + 1), data.Get(cntk));
								}
								break;
							default:
								rtn_value.Add("@" + data.GetKey(0).Replace(".", "___") + "_where_" + (cnti + 1), data.Get(0));
								break;
						}
					}*/
				}
				return rtn_value;
			}
		}

    public class Basic: BQuery {
	    private AZSql azSql;
	    private bool has_schema_data;
	    private AZData data_schema;


      /// <summary>생성자</summary>
      public Basic(string table_name) : base(table_name) {
        //
		    data_schema = null;
		    has_schema_data = false;
      }

      /// <summary>생성자</summary>
      public Basic(string table_name, string connection_json) : base(table_name) {
        //
		    this.azSql = new AZSql(connection_json);
        //
		    data_schema = null;
		    has_schema_data = false;
    		// 지정된 테이블에 대한 스키마 설정
    		//SetSchemaData();
      }

      /// <summary>생성자</summary>
      public Basic(string table_name, string connection_json, bool prepared) : base(table_name, prepared) {
        //
		    this.azSql = new AZSql(connection_json);
        //
		    data_schema = null;
		    has_schema_data = false;
    		// 지정된 테이블에 대한 스키마 설정
    		//SetSchemaData();
      }

      /// <summary>생성자</summary>
      public Basic(string table_name, AZSql azSql): base(table_name) {
        //
		    this.azSql = azSql;
    		//
		    data_schema = null;
		    has_schema_data = false;
    		// 지정된 테이블에 대한 스키마 설정
    		//SetSchemaData();
      }

      /// <summary>생성자</summary>
      public Basic(string table_name, AZSql azSql, bool prepared) : base(table_name, prepared) {
        //
		    this.azSql = azSql;
    		//
		    data_schema = null;
		    has_schema_data = false;
    		// 지정된 테이블에 대한 스키마 설정
    		//SetSchemaData();
      }
    	/// <summary>지정된 테이블에 대한 스키마 정보 설정 처리</summary>
    	private AZSql.Basic SetSchemaData() {
    		if (this.table_name.Trim().Length < 1) {
    			throw new Exception("Target table name not specified.");
    		}
    		switch (this.azSql.GetSqlType()) {
    			case AZSql.SQL_TYPE.MSSQL:
    			case AZSql.SQL_TYPE.MYSQL:
    				try {
    					string mainSql = "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS with (nolock) WHERE TABLE_NAME='" + this.table_name + "';";
    					AZList list = AZSql.Init(this.azSql.db_info).GetList(mainSql);
    					//AZList list = this.azSql.GetList(mainSql);

    					this.data_schema = new AZData();

    					for (int cnti = 0; cnti < list.Size(); cnti++) {
    						AZData data = list.Get(cnti);
    						AZData.AttributeData info = new AZData.AttributeData();
    						for (int cntk = 0; cntk < data.Size(); cntk++) {
    								info.Add(data.GetKey(cntk), data.GetString(cntk));
    						}
    						this.data_schema.Add(data.GetString(0), info);
    					}
    					if (list.Size() > 0) {
    						this.has_schema_data = true;
    					}
    				}
    				catch (Exception) {
    					this.data_schema = null;
    				}
    				break;
    		}
        return this;
    	}
      /// <summary></summary>
      public new AZSql.Basic Set(string p_column, object p_value, VALUETYPE p_valuetype) {
        if (p_column.Trim().Length < 1) {
          throw new Exception("Target column name is not specified.");
        }
        if (HasSchemaData()) {
          if (!this.data_schema.HasKey(p_column)) {
            throw new Exception("Target column name is not exist.");
          }
        }
        AZData data = new AZData();
        data.Attribute.Add(ATTRIBUTE.VALUE, p_valuetype);
        data.Add(p_column, p_value);

        this.sql_set.Add(data);
        data = null;

        return this;
      }
      /// <summary></summary>
      public new AZSql.Basic Where(Or condition) {
        this.sql_where.Add(condition);
        return this;
      }
      /// <summary></summary>
      public new AZSql.Basic Where(And condition) {
        this.sql_where.Add(condition);
        return this;
      }
      /// <summary></summary>
      public new AZSql.Basic Where(Condition condition) {
        if (condition.Column.Trim().Length < 1) {
          throw new Exception("Target column name is not specified.");
        }
        if (HasSchemaData() && !this.data_schema.HasKey(condition.Column)) {
          throw new Exception("Target column name is not exist.");
        }
        this.sql_where.Add(condition);
        return this;
      }
      /// <summary></summary>
      public new AZSql.Basic Where(string p_column, object[] p_values, WHERETYPE p_wheretype, VALUETYPE p_valuetype) {
        if (p_column.Trim().Length < 1) {
          throw new Exception("Target column name is not specified.");
        }
        if (HasSchemaData()) {
          if (!this.data_schema.HasKey(p_column)) {
            throw new Exception("Target column name is not exist.");
          }
        }
        AZData data = new AZData();
        data.Attribute.Add(ATTRIBUTE.WHERE, p_wheretype);
        data.Attribute.Add(ATTRIBUTE.VALUE, p_valuetype);
        for (int cnti = 0; cnti < p_values.Length; cnti++) {
          data.Add(p_column, p_values[cnti]);
        }

        this.sql_where.Add(data);
        data = null;

        return this;
      }
      /// <summary></summary>
      public new AZSql.Basic Where(string p_column, object p_value, WHERETYPE p_wheretype, VALUETYPE p_valuetype) {
        if (p_column.Trim().Length < 1) {
          throw new Exception("Target column name is not specified.");
        }
        if (HasSchemaData()) {
          if (!this.data_schema.HasKey(p_column)) {
            throw new Exception("Target column name is not exist.");
          }
        }
        return Where(new Condition(p_column, p_value, p_wheretype, p_valuetype));
        /*AZData data = new AZData();
        data.Attribute.Add(ATTRIBUTE.WHERE, p_wheretype);
        data.Attribute.Add(ATTRIBUTE.VALUE, p_valuetype);
        data.Add(p_column, p_value);

        this.sql_where.Add(data);
        data = null;

        return this;*/
      }
			/// <summary>주어진 자료를 바탕으로 delete 쿼리 실행</summary>
			public int DoDelete() {
				return DoDelete(true);
			}
			/// <summary>주어진 자료를 바탕으로 delete 쿼리 실행</summary>
			public int DoDelete(bool p_need_where) {
				int rtn_value = -1;
				if (p_need_where && this.sql_where.Count < 1) {
					throw new Exception("Where datas required.");
				}
				if (!IsPrepared()) {
					rtn_value = this.azSql.Execute(GetQuery(CREATE_QUERY_TYPE.DELETE));
				}
				else {
					if (this.azSql == null) {
						throw new Exception("AZSql required.");
					}
					rtn_value = this.azSql.Execute(GetQuery(CREATE_QUERY_TYPE.DELETE), GetPreparedParameters());
				}
				return rtn_value;
			}
			/// <summary>주어진 자료를 바탕으로 update 쿼리 실행</summary>
			public int DoUpdate() {
				return DoUpdate(true);
			}
			/// <summary>주어진 자료를 바탕으로 update 쿼리 실행</summary>
			public int DoUpdate(bool p_need_where) {
				int rtn_value = -1;
				if (this.sql_set.Size() < 1) {
					throw new Exception("Set datas required.");
				}
				if (p_need_where && this.sql_where.Count < 1) {
					throw new Exception("Where datas required.");
				}
				if (!IsPrepared()) {
					rtn_value = this.azSql.Execute(GetQuery(CREATE_QUERY_TYPE.UPDATE));
				}
				else {
					if (this.azSql == null) {
						throw new Exception("AZSql required.");
					}
					rtn_value = this.azSql.Execute(GetQuery(CREATE_QUERY_TYPE.UPDATE), GetPreparedParameters());
				}
				return rtn_value;
			}
			/// <summary>주어진 자료를 바탕으로 insert 쿼리 실행</summary>
			public int DoInsert() {
				return DoInsert(false);
			}
			/// <summary>주어진 자료를 바탕으로 insert 쿼리 실행</summary>
			/// <param name="p_identity">identity값을 받아 올 필요가 있는 경우 true, 아니면 false</param>
			public int DoInsert(bool p_identity) {
				int rtn_value = -1;
				if (this.sql_set.Size() < 1) {
					throw new Exception("Set datas required.");
				}
				if (!IsPrepared()) {
					rtn_value = this.azSql.Execute(GetQuery(CREATE_QUERY_TYPE.INSERT), p_identity);
				}
				else {
					if (this.azSql == null) {
						throw new Exception("AZSql required.");
					}
					rtn_value = this.azSql.Execute(GetQuery(CREATE_QUERY_TYPE.INSERT), GetPreparedParameters());
				}
				return rtn_value;
			}
#if NET_STD || NET_CORE || NET_FX || NET_STORE
			/// <summary>주어진 자료를 바탕으로 delete 쿼리 실행</summary>
			public async Task<int> DoDeleteAsync() {
				return await DoDeleteAsync(true);
			}
			/// <summary>주어진 자료를 바탕으로 delete 쿼리 실행</summary>
			public async Task<int> DoDeleteAsync(bool p_need_where) {
				int rtn_value = -1;
				if (p_need_where && this.sql_where.Count < 1) throw new Exception("Where datas required.");
				if (!IsPrepared()) {
					rtn_value = await this.azSql.ExecuteAsync(GetQuery(CREATE_QUERY_TYPE.DELETE));
				}
				else {
					if (this.azSql == null) throw new Exception("AZSql required.");
					rtn_value = await this.azSql.ExecuteAsync(GetQuery(CREATE_QUERY_TYPE.DELETE), GetPreparedParameters());
				}
				return rtn_value;
			}
			/// <summary>주어진 자료를 바탕으로 update 쿼리 실행</summary>
			public async Task<int> DoUpdateAsync() {
				return await DoUpdateAsync(true);
			}
			/// <summary>주어진 자료를 바탕으로 update 쿼리 실행</summary>
			public async Task<int> DoUpdateAsync(bool p_need_where) {
				int rtn_value = -1;
				if (this.sql_set.Size() < 1) throw new Exception("Set datas required.");
				if (p_need_where && this.sql_where.Count < 1) throw new Exception("Where datas required.");
				if (!IsPrepared()) {
					rtn_value = await this.azSql.ExecuteAsync(GetQuery(CREATE_QUERY_TYPE.UPDATE));
				}
				else {
					if (this.azSql == null) throw new Exception("AZSql required.");
					rtn_value = await this.azSql.ExecuteAsync(GetQuery(CREATE_QUERY_TYPE.UPDATE), GetPreparedParameters());
				}
				return rtn_value;
			}
			/// <summary>주어진 자료를 바탕으로 insert 쿼리 실행</summary>
			public async Task<int> DoInsertAsync() {
				return await DoInsertAsync(false);
			}
			/// <summary>주어진 자료를 바탕으로 insert 쿼리 실행</summary>
			/// <param name="p_identity">identity값을 받아 올 필요가 있는 경우 true, 아니면 false</param>
			public async Task<int> DoInsertAsync(bool p_identity) {
				int rtn_value = -1;
				if (this.sql_set.Size() < 1) throw new Exception("Set datas required.");
				if (!IsPrepared()) {
					rtn_value = await this.azSql.ExecuteAsync(GetQuery(CREATE_QUERY_TYPE.INSERT), p_identity);
				}
				else {
					if (this.azSql == null) throw new Exception("AZSql required.");
					rtn_value = await this.azSql.ExecuteAsync(GetQuery(CREATE_QUERY_TYPE.INSERT), GetPreparedParameters());
				}
				return rtn_value;
			}
#endif
			/// <summary>Prepared 객체를 반환한다</summary>
			public AZSql.Prepared GetPrepared(CREATE_QUERY_TYPE create_query_type) {
				AZSql.Prepared rtn_value = null;
				if (!IsPrepared()) {
					throw new Exception("Property named IsPrepared is not true.");
				}
				if (azSql == null) {
					rtn_value = new AZSql.Prepared();
				}
				else {
					rtn_value = azSql.GetPrepared();
				}
				rtn_value.SetQuery(GetQuery(create_query_type));
				rtn_value.SetParameters(GetPreparedParameters());
				return rtn_value;
			}
			/// <summary>Prepared 객체를 반환한다</summary>
			public AZSql.Prepared GetPrepared(string connection_json, CREATE_QUERY_TYPE create_query_type) {
				AZSql.Prepared rtn_value = null;
				if (!IsPrepared()) {
					throw new Exception("Perperty named IsPrepared is not true.");
				}
				//
				rtn_value = new AZSql(connection_json).GetPrepared();
				rtn_value.SetQuery(GetQuery(create_query_type));
				rtn_value.SetParameters(GetPreparedParameters());
				return rtn_value;
			}
			/// <summary>스키마 데이터를 가지고 있는지 확인 용</summary>
			public bool HasSchemaData() {
				return this.has_schema_data;
			}
			public AZData GetSchemaData() {
				return this.data_schema;
			}
    }

		public class Mongo : BQuery {
			private MongoClient client;
			private IMongoDatabase database;
			private IMongoCollection<BsonDocument> collection;
			private List<OrderData> sorts;

			/// <summary>기본 생성자</summary>
			public Mongo() {
			}

			/// <summary>기본 소멸자</summary>
			~Mongo() {
			}

			/// <summary>생성자, DB연결을 위한 문자열을 통해 연결 생성</summary>
			/// <param name="connection_string"></param>
			public Mongo(string connection_string) {
				SetClient(connection_string);
			}

			/// <summary>생성자, 외부에서 선언된 클라이언트를 참조</summary>
			/// <param name="client"></param>
			public Mongo(ref MongoClient client) {
				this.client = client;
			}

			/// <summary>현재 객체에 대해 DB연결 설정</summary>
			/// <param name="connection_string">DB연결 문자열</param>
			public Mongo SetClient(string connection_string) {
				this.client = new MongoClient(connection_string);
				return this;
			}

			/// <summary>현재 객체에 대해 DB연결 설정</summary>
			/// <param name="client">DB연결 객체 참조값</param>
			public Mongo SetClient(ref MongoClient client) {
				this.client = client;
				return this;
			}

			/// <summary>연결된 DB객체에 대한 database 설정</summary>
			/// <param name="db_string"></param>
			public Mongo SetDB(string db_string) {
				if (this.client == null) {
					throw new Exception("client is null");
				}

				this.database = this.client.GetDatabase(db_string);
				return this;
			}

			/// <summary>database연결 객체 설정</summary>
			/// <param name="db"></param>
			public Mongo SetDB(ref IMongoDatabase db) {
				this.database = db;
				return this;
			}

			/// <summary>
			/// 사전 지정된 database의 collection 지정.
			/// 사전에 지정된 database가 없는 경우 예외 발생됨
			/// </summary>
			/// <param name="collection_string">지정한 collection의 문자열</param>
			public Mongo SetCollection(string collection_string) {
				if (this.client == null || this.database == null) {
					throw new Exception("client/database is null");
				}
				this.collection = this.database.GetCollection<BsonDocument>(collection_string);
				return this;
			}

			/// <summary>collection 객체 설정</summary>
			/// <param name="collection">참조하여 사용할 IMongoCollection 객체</param>
			public Mongo SetCollection(ref IMongoCollection<BsonDocument> collection) {
				this.collection = collection;
				return this;
			}

			/// <summary>INSERT 또는 UPDATE를 하기 위한 컬럼/값 설정</summary>
			/// <param name="column"></param>
			/// <param name="value"></param>
			/// <exception cref="Exception"></exception>
			public new Mongo Set(string column, object value) {
				if (column.Trim().Length < 1) {
					throw new Exception("Target column name is not specified.");
				}
				this.sql_set.Add(new AZData().Add(column, value));
				return this;
			}

			/// <summary>Filter에 사용할 Or 조건 입력</summary>
			/// <param name="condition"></param>
			/// <returns></returns>
			public new Mongo Where(Or condition) {
				this.sql_where.Add(condition);
				return this;
			}

			/// <summary>Filter에 사용할 And 조건 입력</summary>
			/// <param name="condition"></param>
			/// <returns></returns>
			public new Mongo Where(And condition) {
				this.sql_where.Add(condition);
				return this;
			}

			/// <summary>Filter에 사용할 조건 입력</summary>
			/// <param name="condition"></param>
			/// <returns></returns>
			/// <exception cref="Exception"></exception>
			public new Mongo Where(Condition condition) {
				if (condition.Column.Trim().Length < 1) {
					throw new Exception("Target column name is not specified.");
				}

				this.sql_where.Add(condition);
				return this;
			}

			/// <summary>Filter에 사용할 조건 입력</summary>
			/// <param name="column"></param>
			/// <param name="values"></param>
			/// <param name="wheretype"></param>
			/// <returns></returns>
			public new Mongo Where(string column, object[] values, WHERETYPE wheretype) {
				if (column.Trim().Length < 1) {
					throw new Exception("Target column name is not specified.");
				}

				AZData data = new AZData();
				data.Attribute.Add(ATTRIBUTE.WHERE, wheretype);
				for (int cnti = 0; cnti < values.Length; cnti++) {
					data.Add(column, values[cnti]);
				}

				this.sql_where.Add(data);
				data = null;

				return this;
			}

			/// <summary>Filter에 사용할 조건 입력</summary>
			/// <param name="column"></param>
			/// <param name="value"></param>
			/// <param name="wheretype"></param>
			/// <returns></returns>
			public new Mongo Where(string column, object value, WHERETYPE wheretype) {
				if (column.Trim().Length < 1) {
					throw new Exception("Target column name is not specified.");
				}
				return Where(new Condition(column, value, wheretype));
			}

			/// <summary>
			/// 정렬 처리를 위한 자료 입력.
			/// ascending값으로 오름/내림차순 처리 하도록 한다.
			/// 입력되는 순서별로 정렬 처리가 됨
			/// </summary>
			/// <param name="column">정렬 기준 column값</param>
			/// <param name="ascending">오름/내림차순 처리 기준값. true면 오름, false면 내림</param>
			/// <returns></returns>
			public Mongo Order(string column, bool ascending = true) {
				if (sorts == null) sorts = new List<OrderData>();
				sorts.Add(new OrderData(column, ascending));
				return this;
			}

			/// <summary>입력된 조건에 맞는 결과값 갯수 반환</summary>
			/// <param name="size"></param>
			/// <param name="offset"></param>
			public long DoCount(int size = 0, int offset = 0) {
				BsonDocument filter = new BsonDocument();
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
					object row = this.sql_where[cnti];
					if (row.GetType() == typeof(Condition)) {
						filter.AddRange(((Condition) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(And)) {
						filter.AddRange(((And) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(Or)) {
						filter.AddRange(((Or) row).ToBsonDocument());
					}
				}

				var find = this.collection.Find(filter);
				if (size > 0) find.Limit(size);
				if (offset > 0) find.Skip(offset);
				//
				return find.CountDocuments();
			}

			/// <summary>비동기처리. 입력된 조건에 맞는 결과값 갯수 반환</summary>
			/// <param name="size"></param>
			/// <param name="offset"></param>
			public async Task<long> DoCountAsync(int size = 0, int offset = 0) {
				BsonDocument filter = new BsonDocument();
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
					object row = this.sql_where[cnti];
					if (row.GetType() == typeof(Condition)) {
						filter.AddRange(((Condition) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(And)) {
						filter.AddRange(((And) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(Or)) {
						filter.AddRange(((Or) row).ToBsonDocument());
					}
				}
				var find = this.collection.Find(filter);
				if (size > 0) find.Limit(size);
				if (offset > 0) find.Skip(offset);
				//
				return await find.CountDocumentsAsync();
			}

			/// <summary>입력된 조건에 맞는 결과값 목록 반환</summary>
			/// <param name="size">가져올 목록의 최대 갯수 지정. default=0</param>
			/// <param name="offset">가져올 목록의 시작 index 지정. default=0</param>
			public AZList DoSelect(int size = 0, int offset = 0) {
				BsonDocument filter = new BsonDocument();
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
					object row = this.sql_where[cnti];
					if (row.GetType() == typeof(Condition)) {
						filter.AddRange(((Condition) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(And)) {
						filter.AddRange(((And) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(Or)) {
						filter.AddRange(((Or) row).ToBsonDocument());
					}
				}

				var find = this.collection.Find(filter);
				if (size > 0) find.Limit(size);
				if (offset > 0) find.Skip(offset);
				//
				if (this.sorts != null) {
					SortDefinition<BsonDocument> sort = null;
					foreach (OrderData orderData in this.sorts) {
						if (orderData.Column.Trim().Length < 1) continue;
						if (orderData.Ascending) {
							if (sort == null) {
								sort = new SortDefinitionBuilder<BsonDocument>().Ascending(orderData.Column);
							}
							else {
								sort.Ascending(orderData.Column);
							}
						}
						else {
							if (sort == null) {
								sort = new SortDefinitionBuilder<BsonDocument>().Descending(orderData.Column);
							}
							else {
								sort.Descending(orderData.Column);
							}
						}
					}
					if (sort != null) find.Sort(sort);
				}
				return find.ToList().ToAZList();
			}
			
			/// <summary>비동기처리. 입력된 조건에 맞는 결과값 목록 반환</summary>
			/// <param name="size">가져올 목록의 최대 갯수 지정. default=0</param>
			/// <param name="offset">가져올 목록의 시작 index 지정. default=0</param>
			public async Task<AZList> DoSelectAsync(int size = 0, int offset = 0) {
				BsonDocument filter = new BsonDocument();
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
					object row = this.sql_where[cnti];
					if (row.GetType() == typeof(Condition)) {
						filter.AddRange(((Condition) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(And)) {
						filter.AddRange(((And) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(Or)) {
						filter.AddRange(((Or) row).ToBsonDocument());
					}
				}

				var find = this.collection.Find(filter);
				if (size > 0) find.Limit(size);
				if (offset > 0) find.Skip(offset);
				//
				if (this.sorts != null) {
					SortDefinition<BsonDocument> sort = null;
					foreach (OrderData orderData in this.sorts) {
						if (orderData.Column.Trim().Length < 1) continue;
						if (orderData.Ascending) {
							if (sort == null) {
								sort = new SortDefinitionBuilder<BsonDocument>().Ascending(orderData.Column);
							}
							else {
								sort.Ascending(orderData.Column);
							}
						}
						else {
							if (sort == null) {
								sort = new SortDefinitionBuilder<BsonDocument>().Descending(orderData.Column);
							}
							else {
								sort.Descending(orderData.Column);
							}
						}
					}
					if (sort != null) find.Sort(sort);
				}
				return (await find.ToListAsync()).ToAZList();
			}

			/// <summary>Set으로 지정된 컬럼/값 자료를 collection에 입력 처리</summary>
			/// <param name="data">data 인수를 받게 되면 이전의 Set으로 입력된 모든값이 무시되고 data값만 입력됩니다.</param>
			public void DoInsert(AZData data = null) {
				this.collection.InsertOne(data == null ? sql_set.ToBsonDocument() : data.ToBsonDocument());
			}

			/// <summary>비동기처리. Set으로 지정된 컬럼/값 자료를 collection에 입력 처리</summary>
			/// <param name="data">data 인수를 받게 되면 이전의 Set으로 입력된 모든값이 무시되고 data값만 입력됩니다.</param>
			public async Task DoInsertAsync(AZData data = null) {
				await this.collection.InsertOneAsync(data == null ? sql_set.ToBsonDocument() : data.ToBsonDocument());
			}

			/// <summary>Set으로 지정된 컬럼/값 자료를 collection에 입력 처리</summary>
			/// <param name="list"></param>
			public void DoInsertMany(AZList list) {
				this.collection.InsertMany(list.ToBsonDocumentList());
			}

			/// <summary>비동기처리. Set으로 지정된 컬럼/값 자료를 collection에 입력 처리</summary>
			/// <param name="list"></param>
			public async Task DoInsertManyAsync(AZList list) {
				await this.collection.InsertManyAsync(list.ToBsonDocumentList());
			}

			/// <summary>Set으로 지정된 수정값과 Where로 지정된 조건값에 따라 collection의 해당 자료 수정 처리</summary>
			public UpdateResult DoUpdateOne() {
				//
				AZData setData = new AZData();
				for (int cnti = 0; cnti < sql_set.Size(); cnti++) {
					setData.Add(sql_set.Get(cnti));
				}
				BsonDocument set = new BsonDocument("$set", setData.ToBsonDocument());
				//
				BsonDocument filter = new BsonDocument();
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
					object row = this.sql_where[cnti];
					if (row.GetType() == typeof(Condition)) {
						filter.AddRange(((Condition) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(And)) {
						filter.AddRange(((And) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(Or)) {
						filter.AddRange(((Or) row).ToBsonDocument());
					}
				}
				//
				return this.collection.UpdateOne(filter, set);
			}

			/// <summary>비동기처리. Set으로 지정된 수정값과 Where로 지정된 조건값에 따라 collection의 해당 자료 수정 처리</summary>
			/// <summary>비동기처리. Set으로 지정된 수정값과 Where로 지정된 조건값에 따라 collection의 해당 자료 수정 처리</summary>
			public async Task<UpdateResult> DoUpdateOneAsync() {
				//
				AZData setData = new AZData();
				for (int cnti = 0; cnti < sql_set.Size(); cnti++) {
					setData.Add(sql_set.Get(cnti));
				}
				BsonDocument set = new BsonDocument("$set", setData.ToBsonDocument());
				//
				BsonDocument filter = new BsonDocument();
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
					object row = this.sql_where[cnti];
					if (row.GetType() == typeof(Condition)) {
						filter.AddRange(((Condition) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(And)) {
						filter.AddRange(((And) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(Or)) {
						filter.AddRange(((Or) row).ToBsonDocument());
					}
				}
				//
				return await this.collection.UpdateOneAsync(filter, set);
			}

			/// <summary>Set으로 지정된 수정값과 Where로 지정된 조건값에 따라 collection의 해당 자료 수정 처리</summary>
			public UpdateResult DoUpdate() {
				//
				AZData setData = new AZData();
				for (int cnti = 0; cnti < sql_set.Size(); cnti++) {
					setData.Add(sql_set.Get(cnti));
				}
				BsonDocument set = new BsonDocument("$set", setData.ToBsonDocument());
				//
				BsonDocument filter = new BsonDocument();
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
					object row = this.sql_where[cnti];
					if (row.GetType() == typeof(Condition)) {
						filter.AddRange(((Condition) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(And)) {
						filter.AddRange(((And) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(Or)) {
						filter.AddRange(((Or) row).ToBsonDocument());
					}
				}
				//
				return this.collection.UpdateMany(filter, set);
			}

			/// <summary>비동기처리. Set으로 지정된 수정값과 Where로 지정된 조건값에 따라 collection의 해당 자료 수정 처리</summary>
			public async Task<UpdateResult> DoUpdateAsync() {
				//
				AZData setData = new AZData();
				for (int cnti = 0; cnti < sql_set.Size(); cnti++) {
					setData.Add(sql_set.Get(cnti));
				}
				BsonDocument set = new BsonDocument("$set", setData.ToBsonDocument());
				//
				BsonDocument filter = new BsonDocument();
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
					object row = this.sql_where[cnti];
					if (row.GetType() == typeof(Condition)) {
						filter.AddRange(((Condition) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(And)) {
						filter.AddRange(((And) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(Or)) {
						filter.AddRange(((Or) row).ToBsonDocument());
					}
				}
				//
				return await this.collection.UpdateManyAsync(filter, set);
			}

			/// <summary>Where로 지정된 조건값에 따라 collection의 해당 자료 삭제 처리</summary>
			/// <param name="need_where">false인 경우에만 지정된 Where 자료 없이 처리 가능. default=true</param>
			public DeleteResult DoDeleteOne(bool need_where = true) {
				if (need_where && this.sql_where.Count < 1) {
					throw new Exception("Where data required.");
				}
				//
				BsonDocument filter = new BsonDocument();
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
					object row = this.sql_where[cnti];
					if (row.GetType() == typeof(Condition)) {
						filter.AddRange(((Condition) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(And)) {
						filter.AddRange(((And) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(Or)) {
						filter.AddRange(((Or) row).ToBsonDocument());
					}
				}
				return this.collection.DeleteOne(filter);
			}

			/// <summary>비동기처리. Where로 지정된 조건값에 따라 collection의 해당 자료 삭제 처리</summary>
			/// <param name="need_where">false인 경우에만 지정된 Where 자료 없이 처리 가능. default=true</param>
			public async Task<DeleteResult> DoDeleteOneAsync(bool need_where = true) {
				if (need_where && this.sql_where.Count < 1) {
					throw new Exception("Where data required.");
				}
				//
				BsonDocument filter = new BsonDocument();
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
					object row = this.sql_where[cnti];
					if (row.GetType() == typeof(Condition)) {
						filter.AddRange(((Condition) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(And)) {
						filter.AddRange(((And) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(Or)) {
						filter.AddRange(((Or) row).ToBsonDocument());
					}
				}
				return await this.collection.DeleteOneAsync(filter);
			}

			/// <summary>Where로 지정된 조건값에 따라 collection의 해당 자료 삭제 처리</summary>
			/// <param name="need_where">false인 경우에만 지정된 Where 자료 없이 처리 가능. default=true</param>
			public DeleteResult DoDelete(bool need_where = true) {
				if (need_where && this.sql_where.Count < 1) {
					throw new Exception("Where data required.");
				}
				//
				BsonDocument filter = new BsonDocument();
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
					object row = this.sql_where[cnti];
					if (row.GetType() == typeof(Condition)) {
						filter.AddRange(((Condition) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(And)) {
						filter.AddRange(((And) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(Or)) {
						filter.AddRange(((Or) row).ToBsonDocument());
					}
				}
				return this.collection.DeleteMany(filter);
			}

			/// <summary>비동기처리. Where로 지정된 조건값에 따라 collection의 해당 자료 삭제 처리</summary>
			/// <param name="need_where">false인 경우에만 지정된 Where 자료 없이 처리 가능. default=true</param>
			public async Task<DeleteResult> DoDeleteAsync(bool need_where = true) {
				if (need_where && this.sql_where.Count < 1) {
					throw new Exception("Where data required.");
				}
				//
				BsonDocument filter = new BsonDocument();
				for (int cnti = 0; cnti < this.sql_where.Count; cnti++) {
					object row = this.sql_where[cnti];
					if (row.GetType() == typeof(Condition)) {
						filter.AddRange(((Condition) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(And)) {
						filter.AddRange(((And) row).ToBsonDocument());
					}
					else if (row.GetType() == typeof(Or)) {
						filter.AddRange(((Or) row).ToBsonDocument());
					}
				}
				return await this.collection.DeleteManyAsync(filter);
			}

			/// <summary>정렬순서 정보 저장용 객체</summary>
			private class OrderData {
				public string Column { get; set; }
				public bool Ascending { get; set; }
				
				/// <summary>기본 생성자</summary>
				public OrderData() { }

				/// <summary></summary>
				/// <param name="column"></param>
				/// <param name="ascending"></param>
				public OrderData(string column, bool ascending = true) {
					this.Column = column;
					this.Ascending = ascending;
				}
			}
		}
	}

	public static class AZSqlExtend {
		/// <summary></summary>
		/// <param name="source"></param>
		public static BsonDocument ToBsonDocument(this AZData source) {
			BsonDocument rtnValue = new BsonDocument();
			string[] keys = source.GetAllKeys();
			foreach (string column in keys) {
				object value = source.Get(column);
				//
				if (value.GetType().Equals(typeof(Int16))) {
					rtnValue.Add(column, (short)value);
				}
				else if (value.GetType().Equals(typeof(Int32))) {
					rtnValue.Add(column, (int)value);
				}
				else if (value.GetType().Equals(typeof(Int64))) {
					rtnValue.Add(column, (long)value);
				}
				else if (value.GetType().Equals(typeof(AZData))) {
					rtnValue.Add(column, ((AZData)value).ToBsonDocument());
				}
				else if (value.GetType().Equals(typeof(AZList))) {
					rtnValue.Add(column, ((AZList)value).ToBsonArray());
				}
				else {
					rtnValue.Add(column, AZString.Init(value).String());
				}
			}
			return rtnValue;
		}

		/// <summary></summary>
		/// <param name="source"></param>
		public static BsonArray ToBsonArray(this AZList source) {
			BsonArray rtnValue = new BsonArray();
			for (int cnti = 0; cnti < source.Size(); cnti++) {
				rtnValue.Add(source.Get(cnti).ToBsonDocument());
			}
			return rtnValue;
		}

		/// <summary></summary>
		/// <param name="source"></param>
		public static List<BsonDocument> ToBsonDocumentList(this AZList source) {
			List<BsonDocument> rtnValue = new List<BsonDocument>();
			for (int cnti = 0; cnti < source.Size(); cnti++) {
				rtnValue.Add(source.Get(cnti).ToBsonDocument());
			}
			return rtnValue;
		}
		
		/// <summary></summary>
		/// <param name="source"></param>
		public static AZData ToAZData(this BsonDocument source) {
			AZData rtnValue = source.ToString().ToAZData();
			if (rtnValue.HasKey("_id")) {
				string id = rtnValue.GetString("_id");
				if (id.StartsWith("ObjectId(") && id.EndsWith(")")) {
					id = id.Substring(9, id.Length - 10);
					rtnValue.Set("_id", id);
				}
			}
			return source.ToString().ToAZData();
		}
		
		/// <summary></summary>
		/// <param name="source"></param>
		public static AZList ToAZList(this List<BsonDocument> source) {
			AZList rtnValue = new AZList();
			foreach (BsonDocument document in source) {
				rtnValue.Add(document.ToAZData());
			}
			return rtnValue;
		}
	}
}
