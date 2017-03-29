# cs.azlib.core

#### AZSql
```c#
// SELECT 사용법 #1
AZSql sql = new AZSql("DB연결 문자열");
sql.GetData("SELECT id, name FROM T_User WHERE no=1");

// SELECT 사용법 #2
AZSql.Prepared p_sql = new AZSql.Prepared("DB연결 문자열");
p_sql.SetQuery("SELECT id, name FROM T_User WHERE no=@no");
p_sql.AddParam("@no", 1);
p_sql.GetData();

// UPDATE 사용법 #1
AZSql sql = new AZSql("DB연결 문자열");
sql.GetData("UPDATE T_User SET name='이름' WHERE no=1");

// UPDATE 사용법 #2
AZSql.Prepared p_sql = new AZSql.Prepared("DB연결 문자열");
p_sql.SetQuery("UPDATE T_User SET name=@name WHERE no=@no");
p_sql.AddParam("@name", "이름");
p_sql.AddParam("@no", 1);
p_sql.GetData();

// UPDATE 사용법 #3
AZSql.Basic b_sql = new AZSql.Basic("T_User", "DB연결 문자열");
// Prepared Statement 적용을 원하는 경우 SetIsPrepared 메소드를 사용 합니다.
// bSql.SetIsPrepared(true);
b_sql.Set("name", "이름");
b_sql.Where("no", 1);
b_sql.DoUpdate();
```