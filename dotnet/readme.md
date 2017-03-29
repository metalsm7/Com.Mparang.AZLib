# Com.Mparang.AZLib

## AZData
Key:Value 형식의 자료 형식의 객체.
1. Add, Set, Remove 를 통해 간편하게 값을 추가/수정/삭제가 가능
2. JSON 문자열로 출력하거나, JSON 문자열로 부터 AZData 객체 생성 가능
3. Model 객체로 값을 보내거나 가져오기 가능

기본적인 사용법은 아래와 같습니다.

```c#
// Add, Set, Remove
AZData data = new AZData();
data.Add("key1", "value1");
data.Add("key2", "value2");
data.Set("key2", "value2-1");
data.Remove("key1");

// JSON 파싱
string jsonString = data.ToJsonString();
AZData jsonData = AZString.JSON.Init(jsonString).ToAZData();

// 모델 바인딩
Model model = data.Convert<Model>();
AZData dataFromModel = model.To<Model>();   // or AZData dataFromModel = AZData.From<Model>(model);
```

## AZSql
Database 연결 및 데이터 처리를 도와주기 위한 객체.
1. AZData, AZList 객체로 결과값 바인딩
2. PreparedStatement 처리 지원

기본적인 사용법은 아래와 같습니다.

- SELECT 사용
```c#
// DB 연결 문자열 설정
// 형식은 {sql_type:'mssql/mysql/postgresql', connection_string:'각 DB별 연결 문자열'}
String db_con_string = "{sql_type:'mssql', connection_string:'server=127.0.0.1;uid=user;pwd=passwd;database=DB;'}";

// SELECT 사용법 #1
AZSql sql = new AZSql(db_con_string);
sql.GetData("SELECT id, name FROM T_User WHERE no=1");

// SELECT 사용법 #2, Prepared Statement 사용
AZSql.Prepared p_sql = new AZSql.Prepared(db_con_string);
p_sql.SetQuery("SELECT id, name FROM T_User WHERE no=@no");
p_sql.AddParam("@no", 1);   // 각 parameter에 맞는 값을 추가시켜준다
p_sql.GetData();
```

- INSERT 사용
```c#
// INSERT 사용법 #1
AZSql sql = new AZSql(db_con_string);
sql.Execute("INSERT INTO T_User (id, name) VALUES ('userid', '이름')");

// INSERT 사용법 #2, Prepared Statement 사용
AZSql.Prepared p_sql = new AZSql.Prepared(db_con_string);
p_sql.SetQuery("INSERT INTO T_User (id, name) VALUES (@id, @name)");
p_sql.AddParam("@id", "userid");
p_sql.AddParam("@name", "이름");
p_sql.Execute();

// INSERT 사용법 #3
AZSql.Basic b_sql = new AZSql.Basic("T_User", db_con_string);
// Prepared Statement 적용을 원하는 경우 SetIsPrepared 메소드를 사용 합니다.
// bSql.SetIsPrepared(true); // or bSql.IsPrepared = true;
b_sql.Set("@id", "userid");
b_sql.Set("@name", "이름");
b_sql.DoInsert();
```

- UPDATE 사용
```c#
// UPDATE 사용법 #1
AZSql sql = new AZSql(db_con_string);
sql.Execute("UPDATE T_User SET name='이름' WHERE no=1");

// UPDATE 사용법 #2, Prepared Statement 사용
AZSql.Prepared p_sql = new AZSql.Prepared(db_con_string);
p_sql.SetQuery("UPDATE T_User SET name=@name WHERE no=@no");
p_sql.AddParam("@name", "이름");
p_sql.AddParam("@no", 1);
p_sql.Execute();

// UPDATE 사용법 #3
AZSql.Basic b_sql = new AZSql.Basic("T_User", db_con_string);
// Prepared Statement 적용을 원하는 경우 SetIsPrepared 메소드를 사용 합니다.
// bSql.SetIsPrepared(true); // or bSql.IsPrepared = true;
b_sql.Set("name", "이름");
b_sql.Where("no", 1);   // WHERE 메소드는 기본적으로 "=" 조건이 사용됩니다.
b_sql.DoUpdate();

// UPDATE 사용법 #3 - IN 조건
b_sql = new AZSql.Basic("T_User", db_con_string);
b_sql.Set("name", "이름");
b_sql.Where("no", new object[] {1, 2, 3, 4}, AZSql.Basic.WHERETYPE.IN);
b_sql.DoUpdate();

// UPDATE 사용법 #3 - BETWEEN 조건
b_sql = new AZSql.Basic("T_User", db_con_string);
b_sql.Set("name", "이름");
b_sql.Where("no", new object[] {1, 4}, AZSql.Basic.WHERETYPE.BETWEEN);
b_sql.DoUpdate();
```

- DELETE 사용
```c#
// DELETE 사용법 #1
AZSql sql = new AZSql(db_con_string);
sql.Execute("DELETE T_User WHERE no=1");

// DELETE 사용법 #2, Prepared Statement 사용
AZSql.Prepared p_sql = new AZSql.Prepared(db_con_string);
p_sql.SetQuery("DELETE T_User WHERE no=@no");
p_sql.AddParam("@no", 1);
p_sql.Execute();

// DELETE 사용법 #3
AZSql.Basic b_sql = new AZSql.Basic("T_User", db_con_string);
// Prepared Statement 적용을 원하는 경우 SetIsPrepared 메소드를 사용 합니다.
// bSql.SetIsPrepared(true); // or bSql.IsPrepared = true;
b_sql.Where("no", 1);   // WHERE 메소드는 기본적으로 "=" 조건이 사용됩니다.
b_sql.DoDelete();

// DELETE 사용법 #3 - IN 조건
b_sql = new AZSql.Basic("T_User", db_con_string);
b_sql.Where("no", new object[] {1, 2, 3, 4}, AZSql.Basic.WHERETYPE.IN);
b_sql.DoDelete();

// DELETE 사용법 #3 - BETWEEN 조건
b_sql = new AZSql.Basic("T_User", db_con_string);
b_sql.Where("no", new object[] {1, 4}, AZSql.Basic.WHERETYPE.BETWEEN);
b_sql.DoDelete();
```