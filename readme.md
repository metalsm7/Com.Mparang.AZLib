# Com.Mparang.AZLib
- C#, Java에서 동일한 방식으로 데이터 핸들링을 위한 라이브러리 작성이 목표
- C#은 .Net 4.5.2, Standard2.0, CoreApp2.0 를 지원합니다

## Install
1) Package Manager
```
PM> Install-Package Com.Mparang.AZLib
```
2) .NET CLI
```
> dotnet add package Com.Mparang.AZLib
```

## AZData
### Key:Value 형식의 자료형식 객체
- Add, Set, Remove 메소드를 통해 값 추가/수정/삭제
- JSON 문자열로 출력하거나, JSON 문자열로 부터 AZData 객체 생성
- Model 객체로 값을 보내거나 가져오기

기본적인 사용법은 아래와 같습니다.

```c#
// 예제를 위해 선언된 모델 객체
class Model {
    public Model(string name, int num) {
        this.name = name;
        this.num = num;
    }
    public string name { get; set; }
    public int num { get; set; }
}
```
```c#
Model model = new Model("영구", 9);

// 모델 자료를 AZData 자료로 변환 예제
AZData data = AZData.From<Model>(model);
Console.WriteLine("name - " + data.GetString("name")); // name - 영구
Console.WriteLine("num - " + data.GetInt("num"));   // num - 9
// AZData 자료를 json 형식 문자열로 변환합니다.
Console.WriteLine("json - " + data.ToJsonString()); // json - {"name":"영구", "num":"9"}

// AZData 자료를 모델 자료로 변환 예제
Model model_empty = data.Convert<Model>();
Console.WriteLine("name - " + model_empty.name); // name - 영구
Console.WriteLine("num - " + model_empty.num);   // num - 9

// json 문자열로 부터 AZData 자료로 변환 예제
string json_str = "{\"name\":\"영삼\", \"num\":\"3\"}";
AZData data_from_json_str = json_str.ToAZData();
Console.WriteLine("json - " + data_from_json_str.ToJsonString()); // json - {"name":"영삼", "num":"3"}
```

## AZSql
### Database 연결 및 데이터 처리 헬퍼 (mssql, mysql, postgresql, sqlite 지원)
- AZData, AZList 객체로 결과값 바인딩
- PreparedStatement 처리 지원
- StoredProcedure 처리 지원
- Transaction 처리 지원 (처리중 오류 발생시 바로 Rollback 처리되며, 이후 예외처리를 위한 Action을 호출하게 됩니다)

기본적인 사용법은 아래와 같습니다.

```c#
// 예제를 위해 선언된 DB연결 문자열
// 형식은 {sql_type:'mssql/mysql/postgresql', connection_string:'각 DB별 연결 문자열'}
String db_con_string = "{sql_type:'mssql', connection_string:'server=127.0.0.1;uid=user;pwd=passwd;database=DB;'}";
```
```c#
// SELECT 사용법 #1
AZSql sql = new AZSql(db_con_string);
AZData data = sql.GetData("SELECT name, num FROM T_User WHERE no=1");

// SELECT 사용법 #2, Prepared Statement 사용
AZSql.Prepared p_sql = new AZSql.Prepared(db_con_string);
p_sql.SetQuery("SELECT name, num FROM T_User WHERE no=@no");
p_sql.AddParam("@no", 1);
p_sql.GetData();
```
```c#
// INSERT 사용법 #1
AZSql sql = new AZSql(db_con_string);
sql.Execute("INSERT INTO T_User (name, num) VALUES ('이름', 1)");

// INSERT 사용법 #2, Prepared Statement 사용
AZSql ps_sql = new AZSql(db_con_string);
ps_sql.SetQuery("INSERT INTO T_User (name, num) VALUES (@name, @num)");
// AddParameter를 통해서 파라메터 값이 등록되면 자동으로 PreparedStatement 처리를 하게 됩니다
ps_sql.AddParameter("@name", "이름");
ps_sql.AddParameter("@no", 1);
ps_sql.Execute();

// INSERT 사용법 #3, Prepared Statement 사용
AZSql.Prepared p_sql = new AZSql.Prepared(db_con_string);
p_sql.SetQuery("INSERT INTO T_User (id, name) VALUES (@name, @name)");
p_sql.AddParameter("@name", "이름");
p_sql.AddParameter("@no", 1);
p_sql.Execute();

// INSERT 사용법 #4
AZSql.Basic b_sql = new AZSql.Basic("T_User", db_con_string);
// Prepared Statement 적용을 원하는 경우 SetIsPrepared 메소드를 사용 합니다.
// bSql.SetIsPrepared(true); // or bSql.IsPrepared = true;
b_sql.Set("name", "이름");
b_sql.Set("no", 1);
b_sql.DoInsert();
```
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
```c#
// Transaction 사용법
AZSql sql = new AZSql(db_con_string);
sql.BeginTran(
    (ex_on_commit) => Console.WriteLine("Commit 또는 쿼리 처리시 발생된 예외 : " + ex_on_commit.ToString()), 
    (ex_on_rollback) => Console.WriteLine("Rollback 처리시 발생된 예외 : " + ex_on_rollback.ToString())
);
// 순차적으로 쿼리를 처리해 가다가 예외 발생시 자동으로 Rollback 처리 하게 됩니다.
sql.GetData("SELECT name, no FROM T_User with (nolock) WHERE no=1");
sql.Execute("UPDATE T_User SET name='user1' WHERE no=1");[]
sql.Execute("DELETE T_User WHERE no=1");
// 처리 중 발생된 반환값들을 AZData 형식으로 반환 처리 합니다.
AZData result = sql.Commit();
```

## License
    Copyright 2015 Leeyonghun

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.