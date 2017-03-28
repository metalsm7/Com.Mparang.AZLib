# cs.azlib.core

##### *java, .net 에서 특정 동작에 대해 동일한 방식으로 사용하기 위한 라이브러리입니다.*
##### *.net 4.0, 4.5.2, core까지 동시 지원합니다*


#### 기본 자료형 클래스

* AZData<br />
1) key(string):value(object)의 형태를 가집니다. <br />
2) 동일한 키값을 여러개 가질 수 있습니다.<br />
3) key값 또는 index(입력한 순서)값으로 탐색이 가능합니다.<br />
4) 동일한 key값이 여러개인 경우, 최초로 입력된 key값에 대응하는 value값을 반환합니다.<br />
5) json/xml 형식의 문자열로 내보낼 수 있습니다.<br />
6) json 문자열로 부터 자료를 전달받을 수 있습니다.<br />
7) 제네릭형식을 사용하여 반환 데이터 타입을 지정할 수 있습니다.<br />
   (Property명과 key값이 일치하는 경우 Property값을 value값으로 mapping)<br />
8) 제네릭형식을 사용하여 entity객체로 반환할 수 있습니다.<br />
9) 사용예<br />
```c#
// 선언 - 기본
AZData data1 = new AZData();

// 데이타 추가 - 기본
data1.Add("Race", "Human");

// 데이타 추가 - json값 추가
string json_doochi = "{\"Name\":\"Doochi\", \"Age\":\"12\"}";
data1.Add(json_doochi);

// 데이타 추가 - 동일한 키값 추가
data1.Add("Race", "Human??");

// key값으로 자료 반환
Console.WriteLine(data1.Get("Name"));   // return : Doochi

// index값으로 자료 반환
Console.WriteLine(data1.Get(2));   // return : 12

// 동일한 key값이 여러개 있는경우 
// -> 처음 등록된 값만 반환
Console.WriteLine(data1.Get("Race"));   // return : Human

// 동일한 key값이 여러개 있는경우
// -> index를 통해 나중에 등록된 값도 반환 가능
Console.WriteLine(data1.Get(3));   // return : Human??

// json 형식으로 반환
Console.WriteLine(data1.ToJsonString());   
// return : {"Race":"Human", "Name":"Doochi", "Age":"12", "Race":"Human??"}

// generic형식으로 값 반환
int int_value = data1.Get<int>(2);
Console.WriteLine(int_value);   // return : 12

// generic형식으로 entity객체로 반환
/*
class Monster {
    public string Race { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
*/
Monster monster = data1.Convert<Monster>();
Console.WriteLine(monster.Race);   // return : Human
Console.WriteLine(monster.Name);   // return : Doochi
Console.WriteLine(monster.Age);   // return : 12
```


* AZList<br />
1) 내부적으로 List<AZData>를 가집니다.<br />
2) json/xml 형식의 문자열로 내보낼 수 있습니다.<br />
3) 제네릭형식을 사용하여 entity객체 배열로 반환할 수 있습니다.<br />

#### SQL 처리 클래스

* AZSql<br />
현재는 단순히 쿼리문에 대한 결과값 반환 처리만 가능합니다.<br />
*(현재 지원 sql서버: mssql, mssql2k 미지원)*<br />
1) 쿼리문에 대한 단일/단행/다행 결과값 반환<br />
2) 1)의 단행 결과값은 AZData자료형으로 반환<br />
3) 1)의 다행 결과값은 AZList자료형으로 반환<br />
4) 2)3)으로 인해 결과값을 entity객체형으로 변환이 가능<br />
5) 쿼리문 작성 없이 특정 테이블에 대한 INSERT/UPDATE/DELETE 수행<br />
6) 사용예<br />
```c#
// 선언
string con_string = "{sql_type:" + AZSql.SQL_TYPE.MSSQL + ", server:127.0.0.1, id:user, pw:password, catalog:database}";
AZSql sql = new AZSql(new AZSql.DBConnectionInfo(con_string));

// 단일 결과값 반환
Console.WriteLine(sql.Get("SELECT 'Doochi' as Name;"));   // return : Doochi

// 단행 결과값 반환
Console.WriteLine(sql.GetData("SELECT 'Doochi' as Name, '12' as Age, 'Human' as Race;"));
// return : "Name":"Doochi", "Age":"12", "Race":"Human"

// 다행 결과값 반환
Console.WriteLine(sql.Get("SELECT TOP 2 Name, Age, Race FROM Monster;;"));
// return : [{"Name":"Doochi", "Age":"12", "Race":"Human"}, {"Name":"Ppuggu", "Age":"2", "Race":"Dog"}]

// 단행 결과값 반환에 대한 entity객체 mapping
/*
class Monster {
    public string Race { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
*/
Monster monster = sql.GetData("SELECT 'Doochi' as Name, '12' as Age, 'Human' as Race;").Convert<Monster>();
Console.WriteLine(monster.Race);   // return : Human
Console.WriteLine(monster.Name);   // return : Doochi
Console.WriteLine(monster.Age);   // return : 12

// AZSql.Basic 선언
AZSql.Basic basic = new AZSql.Basic("Monster", new AZSql.DBConnectionInfo(con_string));

// INSERT 수행
basic.Set("Name", "Dracula");
basic.Set("Age", "431");
basic.Set("Race", "'Mon' + 's' + 'ter'", AZSql.Basic.VALUETYPE.QUERY); // 쿼리형태로 지정할 때
basic.DoInsert();

// UPDATE 수행
basic.Clear();
basic.Set("Race", "Bat");
basic.Where("Name", "Dracula");
basic.DoUpdate();

// DELETE 수행
basic.Clear();
basic.Where("Name", "Dracula");
basic.DoDelete();
```

#### JSON의 처리

* Parsing

```c#
string json_string = "
  \"key1"\: \"value1\",
  \"key2"\: {\"sub_key1\": \"sub_value1\", \"sub_key2\": \"sub_value2\"},
  \"key3"\: [ {\"list_key1\": \"list_value1\"}, {\"list_key2\": \"list_value2\"}, {\"list_key3\": \"list_value3\"} ]
";
AZData json_data = AZString.JSON.Init(json_string).ToAZData();
```

* JSON으로 변환
```c#
AZData json_data = new AZData();
AZData data_sub = new AZData();
AZList list_sub = new AZList();

data_sub.Add("sub_key1", "sub_value1");
data_sub.Add("sub_key2", "sub_value2");

list_sub.Add(new AZData("list_key1", "list_value1"));
list_sub.Add(new AZData("list_key2", "list_value2"));
list_sub.Add(new AZData("list_key3", "list_value3"));

json_data.Add("key1", "value1");
json_data.Add("key2", data_sub);
json_data.Add("key3", list_sub);

string json_string = json_data.ToJsonString();
```