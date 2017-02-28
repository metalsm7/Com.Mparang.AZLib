/**
 * Copyright (C) <2014~>  <Lee Yonghun, metalsm7@gmail.com, visit http://azlib.mparang.com/>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
//using System.Collections;
using System.Collections.Generic;
using System.Text;
//using System.Data.SQLite;
using System.Data.SqlClient;
#if NETCOREAPP1_0
//using System.Data.SqlClient;
using Npgsql;
using Microsoft.Data.Sqlite;
#endif
//using MySql.Data.MySqlClient;
//using Mono.Data.Sqlite;

namespace Com.Mparang.AZLib {
	public class AZSql {
        public enum SQL_TYPE {
            MYSQL, SQLITE, SQLITE_ANDROID, MSSQL, MARIADB, ORACLE, POSTGRESQL
        }
		public const string SQL_TYPE_MYSQL = "mysql";                       // not using
		public const string SQL_TYPE_SQLITE = "sqlite";                     // Microsoft.Data.Sqlite
		public const string SQL_TYPE_SQLITE_ANDROID = "sqlite_android";     // ?? sqldroid-1.0.3
		public const string SQL_TYPE_MSSQL = "mssql";                       // System.Data.SqlClient
		public const string SQL_TYPE_MARIADB = "mariadb";                   // not using
		public const string SQL_TYPE_ORACLE = "oracle";                     // not using
		public const string SQL_TYPE_POSTGRESQL = "postgresql";             // Npgsql

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

		/*public const string ATTRIBUTE_COLUMN_LABEL = "attribute_column_label";
		public const string ATTRIBUTE_COLUMN_NAME = "attribute_column_name";
		public const string ATTRIBUTE_COLUMN_TYPE = "attribute_column_type";
		public const string ATTRIBUTE_COLUMN_TYPE_NAME = "attribute_column_type_name";
		public const string ATTRIBUTE_COLUMN_SCHEMA_NAME = "attribute_column_schema_name";
		public const string ATTRIBUTE_COLUMN_DISPLAY_SIZE = "attribute_column_display_size";
		public const string ATTRIBUTE_COLUMN_SCALE = "attribute_column_scale";
		public const string ATTRIBUTE_COLUMN_PRECISION = "attribute_column_precision";
		public const string ATTRIBUTE_COLUMN_IS_AUTO_INCREMENT = "attribute_column_auto_increment";
		public const string ATTRIBUTE_COLUMN_IS_CASE_SENSITIVE = "attribute_column_case_sensitive";
		public const string ATTRIBUTE_COLUMN_IS_NULLABLE = "attribute_column_is_nullable";
		public const string ATTRIBUTE_COLUMN_IS_READONLY = "attribute_column_is_readonly";
		public const string ATTRIBUTE_COLUMN_IS_WRITABLE = "attribute_column_is_writable";
		public const string ATTRIBUTE_COLUMN_IS_SIGNED = "attribute_column_is_signed";*/

        private DBConnectionInfo db_info = null;

		private bool connected = false;

		private static AZSql this_object = null;

		//private MySqlConnection mySqlConnection = null;
		private SqlConnection sqlConnection = null;
        
#if NETCOREAPP1_0
		private SqliteConnection sqliteConnection = null;
        private NpgsqlConnection npgsqlConnection = null;
#endif

		//private MySqlCommand mySqlCommand = null;
		private SqlCommand sqlCommand = null;
        
#if NETCOREAPP1_0
		private SqliteCommand sqliteCommand = null;
        private NpgsqlCommand npgsqlCommand = null;
#endif

		public static AZSql getInstance() {
			if (this_object == null) {
				this_object = new AZSql ();
			}
			return this_object;
		}

		public AZSql () {
		}

		public AZSql (string p_json) {
			Set (p_json);
		}

        public AZSql(DBConnectionInfo p_db_connection_info) {
            this.db_info = p_db_connection_info;
        }

		public AZSql Set(string p_json) {
            this.db_info = new DBConnectionInfo(p_json);
			return this;
		}

		public static AZSql Init(string p_json) {
			return new AZSql (p_json);
		}

        public static AZSql Init(DBConnectionInfo p_db_connection_info) {
            return new AZSql(p_db_connection_info);
        }

        /**
         * <summary>
         * </summary>
         * <param name="p_query"> 실행할 쿼리문</param>
         * Created in 2015-06-23, leeyonghun
         */
        public int Execute(string p_query) {
            return Execute(p_query, false);
        }

        /**
         * <summary>
         * </summary>
         * <param name="p_query"> 실행할 쿼리문</param>
         * <param name="p_identity">identity 값을 반환받을 필요가 있을 경우 true, 아니면 false</param>
         * Created in 2015-06-23, leeyonghun
         */
		public int Execute(string p_query, bool p_identity) {
			int rtnValue = 0;

			try {
				Open ();

				if (connected) {
					switch (this.db_info.SqlType) {
					/*case SQL_TYPE.MYSQL:
						mySqlCommand = new MySqlCommand (p_query, mySqlConnection);
                        if (p_identity) {
                            mySqlCommand.ExecuteNonQuery();

                            mySqlCommand = new MySqlCommand("SELECT LAST_INSERT_ID();", mySqlConnection);
                            rtnValue = AZString.Init(mySqlCommand.ExecuteScalar()).ToInt(-1);
                        }
                        else {
                            rtnValue = mySqlCommand.ExecuteNonQuery();
                        }
						break;*/
                    case SQL_TYPE.MSSQL:    // mssql 접속 처리시
                        sqlCommand = new SqlCommand(p_query, sqlConnection);
                        if (p_identity) {
                            sqlCommand.ExecuteNonQuery();

                            sqlCommand = new SqlCommand("SELECT @@IDENTITY;", sqlConnection);
                            rtnValue = AZString.Init(sqlCommand.ExecuteScalar()).ToInt(-1);
                        }
                        else {
                            rtnValue = sqlCommand.ExecuteNonQuery();
                        }
                        break;
#if NETCOREAPP1_0
					case SQL_TYPE.SQLITE:
                        //sqliteCommand = new SQLiteCommand(p_query, sqliteConnection);
                        sqliteCommand = sqliteConnection.CreateCommand();
                        sqliteCommand.CommandText = p_query;
                        if (p_identity) {
                            sqliteCommand.ExecuteNonQuery();

                            sqliteCommand = sqliteConnection.CreateCommand();
                            sqliteCommand.CommandText = "SELECT last_insert_rowid();";
                            rtnValue = AZString.Init(sqliteCommand.ExecuteScalar()).ToInt(-1);
                        }
                        else {
                            rtnValue = sqliteCommand.ExecuteNonQuery();
                        }
                        break;
                    case SQL_TYPE.POSTGRESQL:    // postgresql 접속 처리시
                        npgsqlCommand = new NpgsqlCommand(p_query, npgsqlConnection);
                        if (p_identity) {
                            npgsqlCommand.ExecuteNonQuery();

                            npgsqlCommand = new NpgsqlCommand("SELECT @@IDENTITY;", npgsqlConnection);
                            rtnValue = AZString.Init(npgsqlCommand.ExecuteScalar()).ToInt(-1);
                        }
                        else {
                            rtnValue = npgsqlCommand.ExecuteNonQuery();
                        }
                        break;
#endif
					}
				}
			}
			catch (Exception ex) {
				if (ex.InnerException != null) {
					throw new Exception("Exception in Execute.Inner", ex.InnerException);
				}
				else {
					throw new Exception("Exception in Execute", ex);
				}
			}
			finally {
				Close ();
			}

			return rtnValue;
		}

		public object Get(string p_query) {
			object rtnValue = null;

			try {
				Open ();

				if (connected) {
                    switch (this.db_info.SqlType) {
					/*case SQL_TYPE.MYSQL:
						mySqlCommand = new MySqlCommand (p_query, mySqlConnection);
						rtnValue = mySqlCommand.ExecuteScalar ();
						break;*/
                    case SQL_TYPE.MSSQL:    // mssql 접속 처리시
                        sqlCommand = new SqlCommand(p_query, sqlConnection);
                        rtnValue = sqlCommand.ExecuteScalar();
                        break;
#if NETCOREAPP1_0
					case SQL_TYPE.SQLITE:
                        sqliteCommand = sqliteConnection.CreateCommand();
                        sqliteCommand.CommandText = p_query;
						rtnValue = sqliteCommand.ExecuteScalar();
                        break;
                    case SQL_TYPE.POSTGRESQL:    // postgresql 접속 처리시
                        npgsqlCommand = new NpgsqlCommand(p_query, npgsqlConnection);
                        rtnValue = npgsqlCommand.ExecuteScalar();
                        break;
#endif
					}
                }
                else {
                    throw new Exception("Exception occured in Get : Can not open connection!");
                }
			}
			catch (Exception ex) {
				if (ex.InnerException != null) {
					throw new Exception("Exception in Get.Inner", ex.InnerException);
				}
				else {
					throw new Exception("Exception in Get", ex);
				}
			}
			finally {
				Close ();
			}
			return rtnValue;
		}

        public object GetObject(string p_query) {
            return Get(p_query);
        }

        public int GetInt(string p_query, int p_default_value) {
            return AZString.Init(Get(p_query)).ToInt(p_default_value);
        }

        public int GetInt(string p_query) {
            return GetInt(p_query, 0);
        }

        public float GetFloat(string p_query, float p_default_value) {
            return AZString.Init(Get(p_query)).ToFloat(p_default_value);
        }

        public float GetFloat(string p_query) {
            return GetFloat(p_query, 0f);
        }

        public string GetString(string p_query, string p_default_value) {
            return AZString.Init(Get(p_query)).String(p_default_value);
        }

        public string GetString(string p_query) {
            return GetString(p_query, "");
        }

		public AZData GetData(string p_query) {
			AZData rtnValue = new AZData ();

			//MySqlDataReader reader_mysql = null;
            SqlDataReader reader_mssql = null;
#if NETCOREAPP1_0
            SqliteDataReader reader_sqlite = null;
            NpgsqlDataReader reader_npgsql = null;
#endif

			try {
				Open ();

				if (connected) {
                    switch (this.db_info.SqlType) {
					/*case SQL_TYPE.MYSQL:
						mySqlCommand = new MySqlCommand (p_query, mySqlConnection);
						reader_mysql = mySqlCommand.ExecuteReader ();

						while (reader_mysql.Read()) {
							int colCnt = reader_mysql.FieldCount;

							for (int cnti = 0; cnti < colCnt; cnti++) {
								rtnValue.Add (reader_mysql.GetName (cnti), reader_mysql [cnti]);
							}
							break;
						}
						break;*/
                    case SQL_TYPE.MSSQL:    // mssql 접속 처리시
                        sqlCommand = new SqlCommand(p_query, sqlConnection);
                        reader_mssql = sqlCommand.ExecuteReader();

                        while (reader_mssql.Read()) {
                            int colCnt = reader_mssql.FieldCount;

                            for (int cnti = 0; cnti < colCnt; cnti++) {
                                rtnValue.Add(reader_mssql.GetName(cnti), reader_mssql[cnti]);
                            }
                            break;
                        }
                        break;
#if NETCOREAPP1_0
					case SQL_TYPE.SQLITE:
                        sqliteCommand = sqliteConnection.CreateCommand();
                        sqliteCommand.CommandText = p_query;
						reader_sqlite = sqliteCommand.ExecuteReader();

						while (reader_sqlite.Read()) {
							int colCnt = reader_sqlite.FieldCount;

							for (int cnti = 0; cnti < colCnt; cnti++) {
								rtnValue.Add (reader_sqlite.GetName (cnti), reader_sqlite [cnti]);
							}
							break;
						}
						break;
                    case SQL_TYPE.POSTGRESQL:    // postgresql 접속 처리시
                        npgsqlCommand = new NpgsqlCommand(p_query, npgsqlConnection);
                        reader_npgsql = npgsqlCommand.ExecuteReader();

                        while (reader_npgsql.Read()) {
                            int colCnt = reader_npgsql.FieldCount;

                            for (int cnti = 0; cnti < colCnt; cnti++) {
                                rtnValue.Add(reader_npgsql.GetName(cnti), reader_npgsql[cnti]);
                            }
                            break;
                        }
                        break;
#endif
					}
				}
			}
			catch (Exception ex) {
				if (ex.InnerException != null) {
					throw new Exception("Exception in GetData.Inner", ex.InnerException);
				}
				else {
					throw new Exception("Exception in GetData", ex);
				}
			}
			finally {
				/*if (reader_mysql != null) {
					reader_mysql.Dispose ();
				}*/
                if (reader_mssql != null) {
                    reader_mssql.Dispose();
                }
#if NETCOREAPP1_0
				if (reader_sqlite != null) {
					reader_sqlite.Dispose ();
                }
                if (reader_npgsql != null) {
                    reader_npgsql.Dispose();
                }
#endif
				Close ();
			}

			return rtnValue;
		}

        /**
         * <summary>
         * </summary>
         * Created in 2015-06-24, leeyonghun
         */
        public AZList GetList(string p_query) {
            return GetList(p_query, 0, -1);
        }

        /**
         * <summary>
         * </summary>
         * Created in 2015-06-24, leeyonghun
         */
        public AZList GetList(string p_query, int p_offset) {
            return GetList(p_query, p_offset, p_offset);
        }

        /**
         * <summary>
         * 주어진 쿼리에 대해 offset, length 만큼의 데이터 반환
         * </summary>
         * Created in 2015, leeyonghun
         */
		public AZList GetList(string p_query, int p_offset, int p_length) {
			AZList rtnValue = new AZList ();

			//MySqlDataReader reader_mysql = null;
            SqlDataReader reader_mssql = null;
#if NETCOREAPP1_0
            SqliteDataReader reader_sqlite = null;
            NpgsqlDataReader reader_npgsql = null;
#endif

			try {
				Open ();

                int idx;
				if (connected) {
                    switch (this.db_info.SqlType) {
					    /*case SQL_TYPE.MYSQL:
						    mySqlCommand = new MySqlCommand (p_query, mySqlConnection);
						    reader_mysql = mySqlCommand.ExecuteReader ();

                            idx = 0;    // for check offset
						    while (reader_mysql.Read()) {
                                if (idx < p_offset) {   // 시작점보다 작으면 다음으로.
                                    idx++;  // offset check value update
                                    continue;
                                }
                                if (p_length > 0 && idx >= (p_offset + p_length)) {  // 시작점 + 길이 보다 크면 종료
                                    break;
                                }
							    int colCnt = reader_mysql.FieldCount;
							    AZData data = new AZData ();

							    for (int cnti = 0; cnti < colCnt; cnti++) {
								    data.Add (reader_mysql.GetName (cnti), reader_mysql [cnti]);
							    }
							    rtnValue.Add (data);

                                idx++;  // offset check value update
						    }
						    break;*/
                        case SQL_TYPE.MSSQL:    // mssql 접속 처리시
                            sqlCommand = new SqlCommand(p_query, sqlConnection);
                            reader_mssql = sqlCommand.ExecuteReader();
                            
                            idx = 0;    // for check offset
                            while (reader_mssql.Read()) {
                                if (idx < p_offset) {   // 시작점보다 작으면 다음으로.
                                    idx++;  // offset check value update
                                    continue;
                                }
                                if (p_length > 0 && idx >= (p_offset + p_length)) {  // 시작점 + 길이 보다 크면 종료
                                    break;
                                }
                                int colCnt = reader_mssql.FieldCount;
                                AZData data = new AZData();

                                for (int cnti = 0; cnti < colCnt; cnti++) {
                                    data.Add(reader_mssql.GetName(cnti), reader_mssql[cnti]);
                                }
                                rtnValue.Add(data);

                                idx++;  // offset check value update
                            }
                            break;
#if NETCOREAPP1_0
                        case SQL_TYPE.SQLITE:
                            sqliteCommand = sqliteConnection.CreateCommand();
                            sqliteCommand.CommandText = p_query;
						    reader_sqlite = sqliteCommand.ExecuteReader();
                            
                            idx = 0;    // for check offset
                            while (reader_sqlite.Read()) {
                                if (idx < p_offset) {   // 시작점보다 작으면 다음으로.
                                    idx++;  // offset check value update
                                    continue;
                                }
                                if (p_length > 0 && idx >= (p_offset + p_length)) {  // 시작점 + 길이 보다 크면 종료
                                    break;
                                }
							    int colCnt = reader_sqlite.FieldCount;
							    AZData data = new AZData ();

							    for (int cnti = 0; cnti < colCnt; cnti++) {
								    data.Add (reader_sqlite.GetName (cnti), reader_sqlite [cnti]);
							    }
                                rtnValue.Add(data);

                                idx++;  // offset check value update
						    }
						    break;
                        case SQL_TYPE.POSTGRESQL:    // postgresql 접속 처리시
                            npgsqlCommand = new NpgsqlCommand(p_query, npgsqlConnection);
                            reader_npgsql = npgsqlCommand.ExecuteReader();
                            
                            idx = 0;    // for check offset
                            while (reader_npgsql.Read()) {
                                if (idx < p_offset) {   // 시작점보다 작으면 다음으로.
                                    idx++;  // offset check value update
                                    continue;
                                }
                                if (p_length > 0 && idx >= (p_offset + p_length)) {  // 시작점 + 길이 보다 크면 종료
                                    break;
                                }
                                int colCnt = reader_npgsql.FieldCount;
                                AZData data = new AZData();

                                for (int cnti = 0; cnti < colCnt; cnti++) {
                                    data.Add(reader_npgsql.GetName(cnti), reader_npgsql[cnti]);
                                }
                                rtnValue.Add(data);

                                idx++;  // offset check value update
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
				if (ex.InnerException != null) {
					throw new Exception("Exception in GetList.Inner", ex.InnerException);
				}
				else {
					throw new Exception("Exception in GetList", ex);
				}
			}
			finally {
				/*if (reader_mysql != null) {
					reader_mysql.Dispose ();
				}*/
                if (reader_mssql != null) {
                    reader_mssql.Dispose();
                }
#if NETCOREAPP1_0
				if (reader_sqlite != null) {
					reader_sqlite.Dispose ();
                }
                if (reader_npgsql != null) {
                    reader_npgsql.Dispose();
                }
#endif
				Close ();
			}

			return rtnValue;
		}

		private bool Open() {
			bool rtnValue = false;

            switch (this.db_info.SqlType) {
			/*case SQL_TYPE.MYSQL:
                mySqlConnection = new MySqlConnection(this.db_info.ConnectionString);
				mySqlConnection.Open ();
				rtnValue = true;
				break;*/
			case SQL_TYPE.MSSQL:
                sqlConnection = new SqlConnection(this.db_info.ConnectionString);
				sqlConnection.Open ();
				rtnValue = true;
				break;
#if NETCOREAPP1_0
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

		private bool Close() {
			bool rtnValue = false;

            switch (this.db_info.SqlType) {
			/*case SQL_TYPE.MYSQL:
				if (mySqlConnection.State.Equals (System.Data.ConnectionState.Open)) {
					mySqlConnection.Close ();
				}
				mySqlConnection.Dispose ();
				mySqlConnection = null;
				mySqlCommand.Dispose ();
				mySqlCommand = null;
				rtnValue = true;
				break;*/
			case SQL_TYPE.MSSQL:
				if (sqlConnection.State.Equals (System.Data.ConnectionState.Open)) {
					sqlConnection.Close ();
				}
				sqlConnection = null;
				sqlCommand.Dispose ();
				sqlCommand = null;
				rtnValue = true;
				break;
#if NETCOREAPP1_0
			case SQL_TYPE.SQLITE:
				if (sqliteConnection.State.Equals (System.Data.ConnectionState.Open)) {
					sqliteConnection.Close ();
				}
				sqliteConnection.Dispose ();
				sqliteConnection = null;
				sqliteCommand.Dispose ();
				sqliteCommand = null;
				rtnValue = true;
				break;
			case SQL_TYPE.POSTGRESQL:
				if (npgsqlConnection.State.Equals (System.Data.ConnectionState.Open)) {
					npgsqlConnection.Close ();
				}
				npgsqlConnection = null;
				npgsqlCommand.Dispose ();
				npgsqlCommand = null;
				rtnValue = true;
				break;
#endif
			}

			connected = !rtnValue;

			return rtnValue;
		}

        /**
         * <summary>
         * DB 연결 정보 저장용 객체
         * </summary>
         * 작성일 : 2015-06-03 이용훈
         */
        public class DBConnectionInfo {

            /**
             * <summary>
             * 기본 생성자
             * </summary>
             * 작성일 : 2015-06-03 이용훈
             */
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
				    case SQL_TYPE.MYSQL:
					    ConnectionString = "server=" + Server + ";" + "port=" + Port + ";" +
						    "user=" + ID + ";" + "password=" + PW + ";" + "database=" + Catalog + ";";
					    break;
				    case SQL_TYPE.SQLITE:
					    ConnectionString = "Data Source=" + Server;
					    break;
				    case SQL_TYPE.SQLITE_ANDROID:
					    break;
				    case SQL_TYPE.MSSQL:
                        ConnectionString = "server=" + Server + ";" + (Port > 0 ? ":" + Port : "") + ";" +
						    "uid=" + ID + ";" + "pwd=" + PW + ";" + "database=" + Catalog + ";";
					    break;
				    case SQL_TYPE.MARIADB:
					    break;
				    case SQL_TYPE.ORACLE:
					    break;
				    case SQL_TYPE.POSTGRESQL:
                        ConnectionString = "Host=" + Server + ";Username=" + ID + ";Password=" + PW + ";Database=" + Catalog + ";";
					    break;
				    }
			    }
            }

            /**
             * <summary></summary>
             * Created in 2015-08-19, leeyonghun
             */
            private string GetSqlTypeString(SQL_TYPE p_sql_type) {
                string rtn_value = "";
                switch (p_sql_type) {
                    case SQL_TYPE.MARIADB: rtn_value = "mariadb"; break;
                    case SQL_TYPE.MSSQL: rtn_value = "mssql"; break;
                    case SQL_TYPE.MYSQL: rtn_value = "mysql"; break;
                    case SQL_TYPE.ORACLE: rtn_value = "oracle"; break;
                    case SQL_TYPE.SQLITE: rtn_value = "sqlite"; break;
                    case SQL_TYPE.SQLITE_ANDROID: rtn_value = "sqlite_android"; break;
                    case SQL_TYPE.POSTGRESQL: rtn_value = "postgresql"; break;
                }
                return rtn_value;
            }

            /**
             * <summary></summary>
             * Created in 2015-08-19, leeyonghun
             */
            private SQL_TYPE GetSqlType(string p_sql_type) {
                SQL_TYPE rtn_value = SQL_TYPE.MSSQL;
                switch (p_sql_type.ToLower()) {
                    case "mariadb": rtn_value = SQL_TYPE.MARIADB; break;
                    case "mssql": rtn_value = SQL_TYPE.MSSQL; break;
                    case "mysql": rtn_value = SQL_TYPE.MYSQL; break;
                    case "oracle": rtn_value = SQL_TYPE.ORACLE; break;
                    case "sqlite": rtn_value = SQL_TYPE.SQLITE; break;
                    case "sqlite_android": rtn_value = SQL_TYPE.SQLITE_ANDROID; break;
                    case "postgresql": rtn_value = SQL_TYPE.POSTGRESQL; break;
                }
                return rtn_value;
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

        /**
         * 
         * Created in 2015-06-11, leeyonghun
         */
        public class Query {
            //private Array _select = null;
            //private Array _from = null;
            //private Array _where = null;
            /**
             * 접속사 정보
             * Created in 2015-06-11, leeyonghun
             */
            public enum CONJUNCTION {
                EMPTY, AND, OR
            }

            /**
             * 비교문 정보
             * Created in 2015-06-11, leeyonghun
             */
            public enum COMPARISON {
                EQUAL, NOT_EQUAL,
                GREATER_THAN, GREATER_THAN_OR_EQUAL,
                LESS_THAN, LESS_THAN_OR_EQUAL,
                BETWEEN,
                IN
            }

            public enum VALUETYPE {
                VALUE, QUERY
            }

            /**
             * 조인문 정보
             * Created in 2015-08-10, leeyonghun
             */
            public enum JOIN {
                EMPTY, INNER, CROSS, LEFT_OUTER, RIGHT_OUTER, FULL_OUTER
            }

            /**
             * Created in 2016-05-17, leeyonghun
             */
            public Query() {
            }

            /**
             * Created in 2015-08-10, leeyonghun
             */
            public static string MakeSelect(string p_select, int? p_count, Table p_table, Condition p_condition, Ordering p_order) {
                StringBuilder rtnValue = new StringBuilder();
                if (p_count.HasValue) {
                    rtnValue.AppendFormat("SELECT TOP {0} {1}", p_count, "\r\n");
                }
                else {
                    rtnValue.AppendFormat("SELECT {0}", "\r\n");
                }
                rtnValue.AppendFormat("  {0} {1}", p_select, "\r\n");
                rtnValue.AppendFormat("FROM {0}", "\r\n");
                rtnValue.AppendFormat(" {0} {1}", p_table.GetQuery(), "\r\n");

                //
                if (p_condition != null && p_condition.Size() > 0) {
                    rtnValue.AppendFormat("WHERE {0}", "\r\n");
                    //if (p_condition.GetFirstConjunction() == AZSql.Query.CONJUNCTION.EMPTY) {
                    //    rtnValue.Append("   AND ");
                    //}
                    rtnValue.Append(p_condition.GetQuery());
                }

                //
                if (p_order != null && p_order.Size() > 0) {
                    rtnValue.AppendFormat("ORDER BY {0}", "\r\n");
                    rtnValue.AppendFormat(" {0}", p_order.GetQuery());
                }

                return rtnValue.ToString();
            }

            /**
             * Created in 2015-08-10, leeyonghun
             */
            public class TableData {
                public JOIN Join { get; set; }
                public string Target { get; set; }
                public Condition On { get; set; }

                public TableData() {
                    On = new Condition();
                }

                public static TableData Init() {
                    return new TableData();
                }

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

                public TableData SetJoin(JOIN pValue) { Join = pValue; return this; }
                public TableData SetTarget(string pValue) { Target = pValue; return this; }
                public TableData SetOn(Condition pValue) { On = pValue; return this; }
                public TableData AddOn(ConditionData pValue) {
                    if (this.On == null) {
                        this.On = new Condition();
                    }
                    this.On.Add(pValue);
                    return this; 
                }
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
                /**
                 * <summary></summary>
                 * Created in 2015-08-07, leeyonghun
                 */
                public TableData Clear() {
                    Join = JOIN.EMPTY;
                    Target = "";
                    On.Clear();

                    return this;
                }
            }

            /**
             * Created in 2015-08-10, leeyonghun
             */
            public class Table {
                List<TableData> tableList;

                /**
                 * <summary></summary>
                 * Created in 2015-08-04, leeyonghun
                 */
                public Table() {
                    tableList = new List<TableData>();
                }

                /**
                 * <summary></summary>
                 * Created in 2015-08-04, leeyonghun
                 */
                public static Table Init() {
                    return new Table();
                }

                /**
                 * Created in 2015-08-04, leeyonghun
                 */
                public Table Add(TableData p_value) {
                    this.tableList.Add(p_value);
                    return this;
                }

                /**
                 * Created in 2015-12-17, leeyonghun
                 */
                public TableData Get(int p_value) {
                    return this.tableList[p_value];
                }

                /**
                 * Created in 2015-08-04, leeyonghun
                 */
                public string ToJsonString() {
                    StringBuilder rtnValue = new StringBuilder();
                    rtnValue.Append("[");
                    for (int cnti = 0; cnti < this.tableList.Count; cnti++) {
                        rtnValue.AppendFormat("{0}{1}", cnti > 0 ? "," : "", this.tableList[cnti].ToJsonString());
                    }
                    rtnValue.Append("]");
                    return rtnValue.ToString();
                }

                public JOIN GetFirstJoin() {
                    JOIN rtnValue = JOIN.EMPTY;
                    if (Size() > 0) {
                        rtnValue = tableList[0].Join;
                    }
                    return rtnValue;
                }

                /**
                 * Created in 2015-08-04, leeyonghun
                 */
                public int Size() {
                    return this.tableList.Count;
                }

                /**
                 * Created in 2015-08-04, leeyonghun
                 */
                public string GetQuery() {
                    return AZSql.Query.Table.GetQuery(ToJsonString());
                }

                public static string GetQuery(string p_json) {
                    StringBuilder rtn_value = new StringBuilder();

                    AZList list = AZString.JSON.Init(p_json).ToAZList();
                    AZData query = new AZData();
                    AZData group = new AZData();    // {"group_name":[]}
                    for (int cnti = 0; cnti < list.Size(); cnti++) {
                        AZData data = list.Get(cnti);
                        string data_join = data.GetString("join");
                        string data_target = data.GetString("target");
                        AZList list_on = data.GetList("on");

                        rtn_value.AppendFormat("    {0} {1} {2}", data_join, data_target, "\r\n");
                        if (list_on.Size() > 0) {
                            rtn_value.AppendFormat("        {0} ({1}{2}        ) {3}", "on", "\r\n", AZSql.Query.Condition.GetQuery(list_on.ToJsonString()).Replace("   ", "            "), "\r\n");
                        }
                    }

                    return rtn_value.ToString();
                }

                /**
                 * <summary></summary>
                 * Created in 2015-08-07, leeyonghun
                 */
                public Table Clear() {
                    this.tableList.Clear();
                    return this;
                }
            }

            /**
             * <summary></summary>
             * Created in 2015-08-04, leeyonghun
             */
            public class OrderingData {
                public int Order { get; set; }
                public string Value { get; set; }

                public OrderingData() {
                }

                public OrderingData(int pOrder, string pValue) {
                    Set(pOrder, pValue);
                }

                public static OrderingData Init(int pOrder, string pValue) {
                    return new OrderingData(pOrder, pValue);
                }

                public OrderingData Set(int pOrder, string pValue) {
                    Order = pOrder;
                    Value = pValue;

                    return this;
                }

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

                /**
                 * <summary></summary>
                 * Created in 2015-08-04, leeyonghun
                 */
                public Ordering() {
                    orderingList = new List<OrderingData>();
                }

                /**
                 * <summary></summary>
                 * Created in 2015-08-04, leeyonghun
                 */
                public static Ordering Init() {
                    return new Ordering();
                }

                /**
                 * Created in 2015-08-04, leeyonghun
                 */
                public Ordering Add(OrderingData p_value) {
                    this.orderingList.Add(p_value);
                    return this;
                }

                /**
                 * Created in 2015-08-04, leeyonghun
                 */
                public Ordering Add(int p_order, string p_value) {
                    this.Add(new OrderingData(p_order, p_value));
                    return this;
                }

                /**
                 * Created in 2015-08-04, leeyonghun
                 */
                public string ToJsonString() {
                    StringBuilder rtnValue = new StringBuilder();
                    rtnValue.Append("[");
                    for (int cnti = 0; cnti < this.orderingList.Count; cnti++) {
                        rtnValue.AppendFormat("{0}{1}", cnti > 0 ? "," : "", this.orderingList[cnti].ToJsonString());
                    }
                    rtnValue.Append("]");
                    return rtnValue.ToString();
                }

                /**
                 * Created in 2015-08-04, leeyonghun
                 */
                public string GetQuery() {
                    return AZSql.Query.Ordering.GetQuery(ToJsonString());
                }

                /**
                 * Created in 2015-08-04, leeyonghun
                 */
                public int Size() {
                    return this.orderingList.Count;
                }

                // [{"order":"1~", "value":""},,,]
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

                /**
                 * <summary></summary>
                 * Created in 2015-08-07, leeyonghun
                 */
                public Ordering Clear() {
                    this.orderingList.Clear();
                    return this;
                }
            }

            /**
             * Created in 2015-08-04, leeyonghun
             */
            public class ConditionData {
                public string Group { get; set; }
                public CONJUNCTION Conjunction { get; set; }
                public string Target { get; set; }
                public COMPARISON Comparison { get; set; }
                public List<string> Values { get; set; }

                public ConditionData() {
                    Group = "";
                    Conjunction = CONJUNCTION.EMPTY;
                    Comparison = COMPARISON.EQUAL;
                    Values = new List<string>();
                }

                public static ConditionData Init() {
                    return new ConditionData();
                }

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
                    }
                    return rtnValue;
                }

                public ConditionData SetGroup(string pValue) { Group = pValue; return this; }
                public ConditionData SetConjunction(CONJUNCTION pValue) { Conjunction = pValue; return this; }
                public ConditionData SetTarget(string pValue) { Target = pValue; return this; }
                public ConditionData SetComparison(COMPARISON pValue) { Comparison = pValue; return this; }
                public ConditionData SetValues(List<string> pValue) { Values = Values; return this; }
                public ConditionData SetValue(int pValue) { Values.Clear(); return AddValue("" + pValue); }
                public ConditionData SetValue(float pValue) { Values.Clear(); return AddValue("" + pValue); }
                public ConditionData SetValue(string pValue) { Values.Clear(); return AddValue(pValue); }
                public ConditionData AddValue(int pValue) { AddValue("" + pValue, VALUETYPE.VALUE); return this; }
                public ConditionData AddValue(float pValue) { AddValue("" + pValue, VALUETYPE.VALUE); return this; }
                public ConditionData AddValue(string pValue) { AddValue(pValue, VALUETYPE.VALUE); return this; }
                public ConditionData AddValue(string pValue, VALUETYPE pValueType) { Values.Add(pValueType.Equals(VALUETYPE.VALUE) ? "'" + pValue + "'" : pValue); return this; }
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
                /**
                 * <summary></summary>
                 * Created in 2015-08-07, leeyonghun
                 */
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

                /**
                 * <summary></summary>
                 * Created in 2015-08-04, leeyonghun
                 */
                public Condition() {
                    conditionalList = new List<ConditionData>();
                }

                /**
                 * <summary></summary>
                 * Created in 2015-08-04, leeyonghun
                 */
                public static Condition Init() {
                    return new Condition();
                }

                /**
                 * Created in 2015-08-04, leeyonghun
                 */
                public Condition Add(ConditionData p_value) {
                    this.conditionalList.Add(p_value);
                    return this;
                }

                /**
                 * Created in 2015-08-04, leeyonghun
                 */
                public string ToJsonString() {
                    StringBuilder rtnValue = new StringBuilder();
                    rtnValue.Append("[");
                    for (int cnti = 0; cnti < this.conditionalList.Count; cnti++) {
                        rtnValue.AppendFormat("{0}{1}", cnti > 0 ? "," : "", this.conditionalList[cnti].ToJsonString());
                    }
                    rtnValue.Append("]");
                    return rtnValue.ToString();
                }

                public CONJUNCTION GetFirstConjunction() {
                    CONJUNCTION rtnValue = CONJUNCTION.EMPTY;
                    if (Size() > 0) {
                        rtnValue = conditionalList[0].Conjunction;
                    }
                    return rtnValue;
                }

                /**
                 * Created in 2015-08-04, leeyonghun
                 */
                public int Size() {
                    return this.conditionalList.Count;
                }

                /**
                 * Created in 2015-08-04, leeyonghun
                 */
                public string GetQuery() {
                    return AZSql.Query.Condition.GetQuery(ToJsonString());
                }

                public static string GetQuery(string p_json) {
                    StringBuilder rtn_value = new StringBuilder();

                    AZList list = AZString.JSON.Init(p_json).ToAZList();
                    AZData query = new AZData();
                    AZData group = new AZData();    // {"group_name":[]}
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
                        string tab_string = "   ";

                        // 그룹 내 목록이 1개 초과인 경우
                        if (group_list.Size() > 1) {
                            if (group_key.Equals("_____")) {
                                rtn_value.Append(tab_string + group_list.Get(0).GetString("conjunction") + " ");
                            }
                            else {
                                rtn_value.Append(tab_string + group_list.Get(0).GetString("conjunction") + " (" + "\r\n");
                                tab_string = "      ";
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
                                            rtn_value.Append(group_list.Get(cntk).GetList("values").Get(0).GetString("value") + " AND " + group_list.Get(cntk).GetList("values").Get(1).GetString("value") + "\r\n");
                                        }
                                        else if (group_list.Get(cntk).GetList("values").Size() == 1) {
                                            rtn_value.Append(group_list.Get(cntk).GetList("values").Get(0).GetString("value") + "\r\n");
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
                                        rtn_value.AppendFormat(") {0}", "\r\n");
                                        break;
                                    default:
                                        rtn_value.Append(group_list.Get(cntk).GetList("values").Get(0).GetString("value") + "\r\n");
                                        break;
                                }
                            }

                            if (group_key.Equals("_____")) {
                            }
                            else {
                                rtn_value.Append("  )" + "\r\n");
                            }
                        }
                        else {
                            rtn_value.Append("  " + group_list.Get(0).GetString("conjunction") + " ");
                            string sub_target = group_list.Get(0).GetString("target");
                            string sub_comparison = group_list.Get(0).GetString("comparison");
                            rtn_value.Append(sub_target + " " + sub_comparison + " ");
                            switch (sub_comparison.ToLower()) {
                                case "between":
                                    if (group_list.Get(0).GetList("values").Size() > 1) {
                                        rtn_value.Append(group_list.Get(0).GetList("values").Get(0).GetString("value") + " AND " + group_list.Get(0).GetList("values").Get(1).GetString("value") + "\r\n");
                                    }
                                    else if (group_list.Get(0).GetList("values").Size() == 1) {
                                        rtn_value.Append(group_list.Get(0).GetList("values").Get(0).GetString("value") + "\r\n");
                                    }
                                    break;
                                    //rtn_value.Append(group_list.Get(0).GetList("values").Get(0).GetString("value") + " AND " + group_list.Get(0).GetList("values").Get(1).GetString("value") + "\r\n");
                                //break;
                                case "in":
                                    rtn_value.Append("(");
                                    for (int cntm = 0; cntm < group_list.Get(0).GetList("values").Size(); cntm++) {
                                        if (cntm > 0) {
                                            rtn_value.Append(", ");
                                        }
                                        rtn_value.AppendFormat("{0}", group_list.Get(0).GetList("values").Get(cntm).GetString("value"));
                                    }
                                    rtn_value.AppendFormat(") {0}", "\r\n");
                                    break;
                                default:
                                    rtn_value.Append(group_list.Get(0).GetList("values").Get(0).GetString("value") + "\r\n");
                                    break;
                            }
                        }
                    }
                    return rtn_value.ToString();
                }

                /**
                 * <summary></summary>
                 * Created in 2015-08-07, leeyonghun
                 */
                public Condition Clear() {
                    this.conditionalList.Clear();
                    return this;
                }
            }
        }

        public class Basic {
            public enum WHERETYPE {
                GREATER_THAN, GREATER_THAN_OR_EQUAL, 
                LESS_THAN, LESS_THAN_OR_EQUAL, 
                EQUAL, NOT_EQUAL, 
                BETWEEN, 
                IN
            }
            public enum VALUETYPE {
                VALUE, QUERY
            }
            public enum CREATE_QUERY_TYPE {
                INSERT, UPDATE, DELETE
            }
            private class ATTRIBUTE {
                public const string VALUE = "value";
                public const string WHERE = "where";
            }

            /**
             * <summary></summary>
             * Created in 2015-08-13, leeyonghun
             */
            public class SetList {
                List<SetData> setList;

                /**
                 * <summary>기본생성자</summary>
                 * Created in 2015-08-13, leeyonghun
                 */
                public SetList() {
                    setList = new List<SetData>();
                }

                /**
                 * <summary>인스턴스 생성 후 인스턴스 반환</summary>
                 * Created in 2015-08-13, leeyonghun
                 */
                public static SetList Init() {
                    return new SetList();
                }

                /**
                 * <summary></summary>
                 * Created in 2015-08-13, leeyonghun
                 */
                public SetList Add(SetData p_value) {
                    this.setList.Add(p_value);
                    return this;
                }

                /**
                 * <summary></summary>
                 * Created in 2015-08-13, leeyonghun
                 */
                public SetList Add(string p_column, string p_value, VALUETYPE p_value_type) {
                    return Add(new SetData(p_column, p_value, p_value_type));
                }

                /**
                 * <summary></summary>
                 * Created in 2015-08-13, leeyonghun
                 */
                public SetList Add(string p_column, string p_value) {
                    return Add(new SetData(p_column, p_value));
                }

                /**
                 * <summary></summary>
                 * Created in 2015-08-13, leeyonghun
                 */
                public SetData Get(int p_index) {
                    return this.setList[p_index];
                }

                /**
                 * <summary></summary>
                 * Created in 2015-08-13, leeyonghun
                 */
                public SetData this[int p_index] {
                    get {
                        return Get(p_index);
                    }
                }

                /**
                 * <summary></summary>
                 * Created in 2015-08-13, leeyonghun
                 */
                public string GetQuery() {
                    StringBuilder rtn_value = new StringBuilder();
                    for (int cnti = 0; cnti < this.setList.Count; cnti++) {
                        if (cnti < 1) {
                            rtn_value.AppendFormat("     {0} {1}", this.setList[cnti].GetQuery(), "\r\n");
                        }
                        else {
                            rtn_value.AppendFormat("    ,{0} {1}", this.setList[cnti].GetQuery(), "\r\n");
                        }
                    }
                    return rtn_value.ToString();
                }

                /**
                 * <summary></summary>
                 * Created in 2015-08-13, leeyonguhn
                 */
                public int Size() {
                    return this.setList.Count;
                }
            }

            /**
             * <summary></summary>
             * Created in 2015-08-12, leeyonghun
             */
            public class SetData {
                public string Column { get; set; }
                public string Value { get; set; }
                public VALUETYPE ValueType { get; set; }

                /**
                 * <summary>기본생성자</summary>
                 * Created in 2015-08-12, leeyonghun
                 */
                public SetData() {
                    ValueType = VALUETYPE.VALUE;
                }

                /**
                 * <summary>인스턴스 생성 후 인스턴스 반환 처리</summary>
                 * Created in 2015-08-13, leeyonghun
                 */
                public static SetData Init() {
                    return new SetData();
                }
                
                /**
                 * <summary>기본생성자</summary>
                 * Created in 2015-08-12, leeyonghun
                 */
                public SetData(string p_column, string p_value) {
                    Set(p_column, p_value, VALUETYPE.VALUE);
                }
                
                /**
                 * <summary>기본생성자</summary>
                 * Created in 2015-08-12, leeyonghun
                 */
                public SetData(string p_column, string p_value, VALUETYPE p_value_type) {
                    Set(p_column, p_value, p_value_type);
                }
                
                /**
                 * <summary>기본값 설정</summary>
                 * Created in 2015-08-12, leeyonghun
                 */
                public void Set(string p_column, string p_value, VALUETYPE p_value_type) {
                    this.Column = p_column;
                    this.Value = p_value;
                    this.ValueType = p_value_type;
                }

                /**
                 * <summary></summary>
                 * Created in 2015-08-13, leeyonghun
                 */
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

            private string table_name;
            private DBConnectionInfo db_info;
            private AZList sql_where, sql_set;
            private AZData data_schema;
            //private string query;
            private bool has_schema_data;
            /**
             * <summary>
             * Basic constructor
             * </summary>
             * Created : 2015-06-02, leeyonghun
             */
            public Basic(string p_table_name, DBConnectionInfo p_db_connection_info) {
                if (p_table_name.Trim().Length < 1) {
                    throw new Exception("Target table name not specified.");
                }
                this.table_name = AZString.Encode(AZString.ENCODE.JSON, p_table_name);
                this.db_info = p_db_connection_info;

                sql_where = new AZList();
                sql_set = new AZList();
                data_schema = null;

                has_schema_data = false;

                //query = "";

                // 지정된 테이블에 대한 스키마 설정
                SetSchemaData();
            }

            /**
             * <summary>
             * basic constructor
             * </summary>
             * Created in 2015-06-23, leeyonghun
             */
            public Basic(string p_table_name) {
                if (p_table_name.Trim().Length < 1) {
                    throw new Exception("Target table name not specified.");
                }
                this.table_name = AZString.Encode(AZString.ENCODE.JSON, p_table_name);

                sql_where = new AZList();
                sql_set = new AZList();
                data_schema = null;

                has_schema_data = false;

                //query = "";
            }

            /**
             * <summary>
             * Creating new class and return
             * </summary>
             * Created : 2015-06-02, leeyonghun
             */
            public static AZSql.Basic Init(string p_table_name, DBConnectionInfo p_db_connection_info) {
                if (p_table_name.Trim().Length < 1) {
                    throw new Exception("Target table name not specified.");
                }
                return new AZSql.Basic(p_table_name, p_db_connection_info);
            }

            /**
             * <summary>
             * 지정된 테이블에 대한 스키마 정보 설정 처리
             * </summary>
             * Created : 2015-06-03 이용훈
             */
            private void SetSchemaData() {
                if (this.table_name.Trim().Length < 1) {
                    throw new Exception("Target table name not specified.");
                }
                switch (this.db_info.SqlType) {
                    case AZSql.SQL_TYPE.MSSQL:
                    case AZSql.SQL_TYPE.MYSQL:
                        try {
                            string mainSql = "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS with (nolock) WHERE TABLE_NAME='" + this.table_name + "';";
                            AZList list = AZSql.Init(this.db_info).GetList(mainSql);

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
            }

            /**
             * <summary></summary>
             * Created in 2015-08-19, leeyonghun
             */
            public void Clear() {
                this.sql_set.Clear();
                this.sql_where.Clear();
            }

            /**
             * <summary></summary>
             * Created in 2015-08-12, leeyonghun
             */
            public AZSql.Basic Set(SetData p_set_data) {
                if (p_set_data != null) {
                    Set(p_set_data.Column, p_set_data.Value, p_set_data.ValueType);
                }
                return this;
            }

            /**
             * <summary></summary>
             * Created in 2015-08-12, leeyonghun
             */
            public AZSql.Basic Set(SetData[] p_set_datas) {
                if (p_set_datas != null) {
                    for (int cnti = 0; cnti < p_set_datas.Length; cnti++) {
                        if (p_set_datas[cnti] != null) {
                            Set(p_set_datas[cnti]);
                        }
                    }
                }
                return this;
            }

            /**
             * <summary></summary>
             * Created in 2015-08-12, leeyonghun
             */
            public AZSql.Basic Set(SetList p_set_list) {
                if (p_set_list != null) {
                    for (int cnti = 0; cnti < p_set_list.Size(); cnti++) {
                        if (p_set_list.Get(cnti) != null) {
                            Set(p_set_list.Get(cnti));
                        }
                    }
                }
                return this;
            }

            /**
             * <summary></summary>
             * Created : 2015-06-03, leeyonghun
             */
            public AZSql.Basic Set(string p_column, object p_value) {
                return Set(p_column, p_value, VALUETYPE.VALUE);
            }

            /**
             * <summary></summary>
             * Created : 2015-06-03, leeyonghun
             */
            public AZSql.Basic Set(string p_column, object p_value, VALUETYPE p_valuetype) {
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

            /**
             * <summary></summary>
             * Created : 2015-06-03, leeyonghun
             */
            public AZSql.Basic Where(string p_column, object p_value) {
                return Where(p_column, p_value, WHERETYPE.EQUAL, VALUETYPE.VALUE);
            }

            /**
             * <summary></summary>
             * Created : 2015-06-03, leeyonghun
             */
            public AZSql.Basic Where(string p_column, object p_value, WHERETYPE p_wheretype) {
                return Where(p_column, p_value, p_wheretype, VALUETYPE.VALUE);
            }

            /**
             * <summary></summary>
             * Created : 2015-06-03, leeyonghun
             */
            public AZSql.Basic Where(string p_column, object p_value, WHERETYPE p_wheretype, VALUETYPE p_valuetype) {
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
                data.Add(p_column, p_value);

                this.sql_where.Add(data);
                data = null;

                return this;
            }

            /**
             * <summary></summary>
             * Created : 2015-06-03, leeyonghun
             */
            public AZSql.Basic Where(string p_column, object[] p_value) {
                return Where(p_column, p_value, WHERETYPE.EQUAL, VALUETYPE.VALUE);
            }

            /**
             * <summary></summary>
             * Created : 2015-06-03, leeyonghun
             */
            public AZSql.Basic Where(string p_column, object[] p_value, WHERETYPE p_wheretype) {
                return Where(p_column, p_value, p_wheretype, VALUETYPE.VALUE);
            }

            /**
             * <summary></summary>
             * Created : 2015-06-03, leeyonghun
             */
            public AZSql.Basic Where(string p_column, object[] p_values, WHERETYPE p_wheretype, VALUETYPE p_valuetype) {
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

            /**
             * <summary>
             * 특정된 쿼리 타입에 맞게 현재의 자료를 바탕으로 쿼리 문자열 생성
             * </summary>
             * Created : 2015-06-04, leeyonghun
             */
            private string CreateQuery(CREATE_QUERY_TYPE p_type) {
                StringBuilder rtn_value = new StringBuilder();
                switch (p_type) {
                    case CREATE_QUERY_TYPE.INSERT:
                        rtn_value.Append("INSERT INTO " + table_name + " ( " + "\r\n");
                        for (int cnti = 0; cnti < this.sql_set.Size(); cnti++) {
                            AZData data = this.sql_set.Get(cnti);
                            rtn_value.Append("  " + (cnti > 0 ? ", " : "") + data.GetKey(0));
                        }
                        rtn_value.Append("\r\n" + ") " + "\r\n");
                        rtn_value.Append("VALUES ( " + "\r\n");
                        for (int cnti = 0; cnti < this.sql_set.Size(); cnti++) {
                            AZData data = this.sql_set.Get(cnti);
                            if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.QUERY)) {
                                rtn_value.Append("  " + (cnti > 0 ? ", " : "") + data.GetString(0) + "\r\n");
                            }
                            else if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.VALUE)) {
                                rtn_value.Append("  " + (cnti > 0 ? ", " : "") + "'" + data.GetString(0) + "'" + "\r\n");
                            }
                        }
                        rtn_value.Append(")");
                        break;
                    case CREATE_QUERY_TYPE.UPDATE:
                        rtn_value.Append("UPDATE " + table_name + " " + "\r\n");
                        rtn_value.Append("SET " + "\r\n");
                        for (int cnti = 0; cnti < this.sql_set.Size(); cnti++) {
                            AZData data = this.sql_set.Get(cnti);
                            if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.QUERY)) {
                                rtn_value.Append("  " + (cnti > 0 ? ", " : "") + data.GetKey(0) + " = " + data.GetString(0) + "\r\n");
                            }
                            else if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.VALUE)) {
                                rtn_value.Append("  " + (cnti > 0 ? ", " : "") + data.GetKey(0) + " = " + "'" + data.GetString(0) + "'" + "\r\n");
                            }
                        }
                        for (int cnti = 0; cnti < this.sql_where.Size(); cnti++) {
                            if (cnti < 1) {
                                rtn_value.Append("WHERE " + "\r\n");
                            }
                            AZData data = this.sql_where.Get(cnti);
                            if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.QUERY)) {
                                rtn_value.Append("  " + (cnti > 0 ? " AND " : "") + data.GetKey(0));

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
                                        rtn_value.Append(", " + data.GetString(cntk));
                                    }
                                    rtn_value.Append(" ) ");
                                }
                                rtn_value.Append("\r\n");
                            }
                            else if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.VALUE)) {
                                rtn_value.Append("  " + (cnti > 0 ? " AND " : "") + data.GetKey(0));

                                if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.EQUAL)) {
                                    rtn_value.Append(" = ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.GREATER_THAN)) {
                                    rtn_value.Append(" > ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.GREATER_THAN_OR_EQUAL)) {
                                    rtn_value.Append(" >= ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.LESS_THAN)) {
                                    rtn_value.Append(" < ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.LESS_THAN_OR_EQUAL)) {
                                    rtn_value.Append(" <= ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.NOT_EQUAL)) {
                                    rtn_value.Append(" <> ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.BETWEEN)) {
                                    rtn_value.Append(" BETWEEN ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                    if (data.Size() > 1) {
                                        rtn_value.Append(" AND " + "'" + data.GetString(0) + "'");
                                    }
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.IN)) {
                                    rtn_value.Append(" IN ( ");
                                    for (int cntk = 0; cntk < data.Size(); cntk++) {
                                        rtn_value.Append(", " + "'" + data.GetString(0) + "'");
                                    }
                                    rtn_value.Append(" ) ");
                                }
                                rtn_value.Append("\r\n");
                            }
                        }
                        break;
                    case CREATE_QUERY_TYPE.DELETE:
                        rtn_value.Append("DELETE FROM " + table_name + " " + "\r\n");
                        for (int cnti = 0; cnti < this.sql_where.Size(); cnti++) {
                            if (cnti < 1) {
                                rtn_value.Append("WHERE " + "\r\n");
                            }
                            AZData data = this.sql_where.Get(cnti);
                            if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.QUERY)) {
                                rtn_value.Append("  " + (cnti > 0 ? " AND " : "") + data.GetKey(0));

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
                                        rtn_value.Append(", " + data.GetString(cntk));
                                    }
                                    rtn_value.Append(" ) ");
                                }
                                rtn_value.Append("\r\n");
                            }
                            else if (data.Attribute.Get(ATTRIBUTE.VALUE).Equals(VALUETYPE.VALUE)) {
                                rtn_value.Append("  " + (cnti > 0 ? " AND " : "") + data.GetKey(0));

                                if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.EQUAL)) {
                                    rtn_value.Append(" = ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.GREATER_THAN)) {
                                    rtn_value.Append(" > ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.GREATER_THAN_OR_EQUAL)) {
                                    rtn_value.Append(" >= ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.LESS_THAN)) {
                                    rtn_value.Append(" < ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.LESS_THAN_OR_EQUAL)) {
                                    rtn_value.Append(" <= ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.NOT_EQUAL)) {
                                    rtn_value.Append(" <> ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.BETWEEN)) {
                                    rtn_value.Append(" BETWEEN ");
                                    rtn_value.Append("'" + data.GetString(0) + "'");
                                    if (data.Size() > 1) {
                                        rtn_value.Append(" AND " + "'" + data.GetString(0) + "'");
                                    }
                                }
                                else if (data.Attribute.Get(ATTRIBUTE.WHERE).Equals(WHERETYPE.IN)) {
                                    rtn_value.Append(" IN ( ");
                                    for (int cntk = 0; cntk < data.Size(); cntk++) {
                                        rtn_value.Append(", " + "'" + data.GetString(0) + "'");
                                    }
                                    rtn_value.Append(" ) ");
                                }
                                rtn_value.Append("\r\n");
                            }
                        }
                        break;
                }
                return rtn_value.ToString();
            }

            /**
             * <summary>
             * 주어진 자료를 바탕으로 delete 쿼리 실행
             * </summary>
             * Created : 2015-06-04, leeyonghun
             */
            public int DoDelete() {
                return DoDelete(true);
            }

            /**
             * <summary>
             * 주어진 자료를 바탕으로 delete 쿼리 실행
             * </summary>
             * Created : 2015-06-04, leeyonghun
             */
            public int DoDelete(bool p_need_where) {
                int rtn_value = -1;
                if (p_need_where && this.sql_where.Size() < 1) {
                    throw new Exception("Where datas required.");
                }
                rtn_value = AZSql.Init(this.db_info).Execute(GetQuery(CREATE_QUERY_TYPE.DELETE));
                return rtn_value;
            }

            /**
             * <summary>
             * 주어진 자료를 바탕으로 update 쿼리 실행
             * </summary>
             * Created : 2015-06-04, leeyonghun
             */
            public int DoUpdate() {
                return DoUpdate(true);
            }

            /**
             * <summary>
             * 주어진 자료를 바탕으로 update 쿼리 실행
             * </summary>
             * Created : 2015-06-04, leeyonghun
             */
            public int DoUpdate(bool p_need_where) {
                int rtn_value = -1;
                if (this.sql_set.Size() < 1) {
                    throw new Exception("Set datas required.");
                }
                if (p_need_where && this.sql_where.Size() < 1) {
                    throw new Exception("Where datas required.");
                }
                rtn_value = AZSql.Init(this.db_info).Execute(GetQuery(CREATE_QUERY_TYPE.UPDATE));
                return rtn_value;
            }

            /**
             * <summary>
             * 주어진 자료를 바탕으로 insert 쿼리 실행
             * </summary>
             * Created : 2015-06-04, leeyonghun
             */
            public int DoInsert() {
                return DoInsert(false);
            }

            /**
             * <summary>
             * 주어진 자료를 바탕으로 insert 쿼리 실행
             * </summary>
             * <param name="p_identity">identity값을 받아 올 필요가 있는 경우 true, 아니면 false</param>
             * Created : 2015-06-04, leeyonghun
             */
            public int DoInsert(bool p_identity) {
                int rtn_value = -1;
                if (this.sql_set.Size() < 1) {
                    throw new Exception("Set datas required.");
                }
                rtn_value = AZSql.Init(this.db_info).Execute(GetQuery(CREATE_QUERY_TYPE.INSERT), p_identity);
                return rtn_value;
            }

            /**
             * <summary>
             * 특정된 쿼리 실행 종류에 맞는 쿼리 문자열 생성 후 반환
             * </summary>
             * Created : 2015-06-03 leeyonghun
             */
            public string GetQuery(CREATE_QUERY_TYPE p_create_query_type) {
                return CreateQuery(p_create_query_type);
            }

            /**
             * <summary>
             * 스키마 데이터를 가지고 있는지 확인 용
             * </summary>
             * Created : 2015-06-03 leeyonghun
             */
            public bool HasSchemaData() {
                return this.has_schema_data;
            }

            /**
             * <summary>
             * </summary>
             * Created : 2015-06-03 leeyonghun
             */
            public AZData GetSchemaData() {
                return this.data_schema;
            }
        }
	}
}