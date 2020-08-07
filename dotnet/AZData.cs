using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Com.Mparang.AZLib {
	public class AZData : IEnumerator, IEnumerable {
		private Dictionary<string, object> map_async = null;
		private AttributeData attribute_data = null;
		private List<KeyLink> indexer = null;
		private static object lockObject;
		public string Name { get; set; }
		public string Value { get; set; }
		// IEnumerable 용
		private int index = -1;

		/// <summary>기본 생성자</summary>
		public AZData() {
			if (lockObject == null) lockObject = new object();
			//
			map_async = new Dictionary<string, object>();
			attribute_data = new AttributeData();
			indexer = new List<KeyLink>();
		}
	
		public AttributeData Attribute {
			get { return attribute_data; }
		}
	
		/// <summary>모델 객체로부터 AZData를 생성</summary>
		/// <param name="source">AZData로 변경할 모델 객체</param>
		public static AZData From<T>(T source) {
			AZData rtnValue = new AZData();
#if NET_STD || NET_CORE	|| NET_STORE
			Type type = typeof(T);
			IEnumerable<PropertyInfo> properties = type.GetRuntimeProperties();
			foreach (PropertyInfo property in properties) {
				if (!property.CanRead) { continue; }
				rtnValue.Add(property.Name, property.Name.Equals("SyncRoot") ? property.GetValue(source, null).ToString() : property.GetValue(source, null));
			}
#endif
#if NET_FX
			Type type = typeof(T);
			System.Reflection.PropertyInfo[] properties = type.GetProperties();
			for (int cnti = 0; cnti < properties.Length; cnti++) {
				System.Reflection.PropertyInfo property = properties[cnti];
				if (!property.CanRead) continue;
				// ICollection 구현체에 대한 재귀 오류 수정처리, 2016-05-19,, leeyonghun
				rtnValue.Add(property.Name, property.Name.Equals("SyncRoot") ? property.GetValue(source, null).ToString() : property.GetValue(source, null));
			}
#endif
			return rtnValue;
		}

		/// <summary>모델 객체로부터 AZData를 생성</summary>
		/// <param name="source">AZData로 변경할 모델 객체</param>
		public static AZData Parse(string source) {
			return AZString.JSON.ToAZData(source);
		}

		/// <summary>json 형식의 문자열을 AZData로 변경, 이 자료를 현재의 자료에 추가</summary>
		/// <param name="json">string, json형식의 문자열</param>
		public AZData Add(string json) {
			AZData data_json = AZString.JSON.Init(json).ToAZData();
			for (int cnti = 0; cnti < data_json.Size(); cnti++) {
				Add(data_json.GetKey(cnti), data_json.Get(cnti));
			}
			return this;
		}
	
		/// <summary>현재의 자료에 입력받은 자료를 추가</summary>
		/// <param name="value">AZData, 추가할 AZData 자료</param>
		public AZData Add(AZData value) {
			for (int cnti = 0; cnti < value.Size(); cnti++) {
				Add(value.GetKey(cnti), value.Get(cnti));
			}
			return this;
		}
		
		/// <summary>key, value 자료를 입력받아 현재의 자료에 추가</summary>
		/// <param name="key">string, key값</param>
		/// <param name="value">object, kye에 해당하는 자료값</param>
		public AZData Add(string key, object value) {
			lock (lockObject) {
				if (map_async.ContainsKey(key)) {
					// 동일 키값이 이미 존재하는 경우
					string linkString = AZString.Random(20);
					map_async.Add(linkString, value);
					indexer.Add(new KeyLink(key, linkString));
				}
				else {
					map_async.Add(key, value);
					indexer.Add(new KeyLink(key, key));
				}
			}
			return this;
		}

		/// <summary>
		/// 현재에 자료와 통합, 동일 key값이 존재할 때, overwrite가 true이면 새로운 값으로 교체, 아닌경우 기존값 유지
		/// </summary>
		/// <param name="value"></param>
		/// <param name="overwrite"></param>
		/// <returns></returns>
		public AZData Merge(AZData value, bool overwrite = false) {
			for (int cnti = 0; cnti < value.Size(); cnti++) {
				if (HasKey(value.GetKey(cnti))) {
					if (overwrite) Set(value.GetKey(cnti), value.Get(cnti));
				}
				else {
					Add(value.GetKey(cnti), value.Get(cnti));
				}
			}
			return this;
		}

		/// <summary>현재 객체가 가지고 있는 모든 키의 목록을 반환</summary>
		public string[] GetAllKeys() {
			string[] rtnValue = new string[indexer.Count];
			for (int cnti = 0; cnti < indexer.Count; cnti++) {
				rtnValue[cnti] = indexer[cnti].GetKey();
			}
			return rtnValue;
		}
		
		/// <summary>현재의 자료 중 지정된 순서에 해당하는 자료를 반환</summary>
		/// <param name="index">int, 반환할 자료의 index 값, zero base</param>
		public object Get(int index) {
			if (index >= indexer.Count) {
				return null;
			}
			else {
				return map_async[indexer[index].GetLink()]; 
			}
		}
	
		/// <summary>현재의 자료 중 key값과 일치하는 자료를 반환</summary>
		/// <param name="key">string, 반환할 자료의 key값</param>
		public object Get(string key) {
			if (map_async.ContainsKey(key)) {
				return map_async[key]; 
			}
			else {
				return null;
			}
		}
	
		/// <summary>현재의 자료 중 지정된 순서에 해당하는 자료를 반환</summary>
		/// <param name="index">int, 반환할 자료의 index 값, zero base</param>
		public object this[int index] {
			get { return Get(index); }
			set { Set(index, value); }
		}
	
		/// <summary>현재의 자료 중 key값과 일치하는 자료를 반환</summary>
		/// <param name="key">string, 반환할 자료의 key값</param>
		public object this[string key] {
			get { return Get(key); }
			set { Set(key, value); }
		}
	
		/// <summary>IEnumerable 구현</summary>
		public bool MoveNext() {
			this.index++;
			return (this.index < Size());
		}
	
		/// <summary>IEnumerable 구현</summary>
		public void Reset() {
			this.index = -1;
		}
	
		/// <summary>IEnumerable 구현</summary>
		object IEnumerator.Current {
			get { return Current; }
		}
	
		/// <summary>IEnumerable 구현</summary>
		IEnumerator IEnumerable.GetEnumerator() {
			return map_async.GetEnumerator();
		}
	
		/// <summary>IEnumerable 구현</summary>
		public object Current {
			get {
				try {
					return Get(this.index);
				}
				catch (IndexOutOfRangeException) {
					throw new InvalidOperationException();
				}
			}
		}
	
		/// <summary>Get 메소드의 generic 형</summary>
		/// <param name="index">int, 반환할 자료의 index 값, zero base</param>
		public T Get<T>(int index) {
			object rtnValue;
			if (typeof(T).Name.Equals(typeof(String).Name)) {
				rtnValue = "";
			}
			else {
				rtnValue = Activator.CreateInstance(typeof(T));
			}
			rtnValue = Get(index);
			if (rtnValue != null) {
				try {
					rtnValue = AZString.Init(rtnValue).To<T>();
				}
				catch (Exception) { }
			}
			return (T)rtnValue;
		}
	
		/// <summary>Get 메소드의 generic 형</summary>
		/// <param name="index">int, 반환할 자료의 index 값, zero base</param>
		/// <param name="defaultValue">T, 자료 반환시 예외 발생시 반환받을 기본값</param>
		public T Get<T>(int index, T defaultValue) {
			object rtnValue;
			if (typeof(T).Name.Equals(typeof(String).Name)) {
				rtnValue = "";
			}
			else {
				rtnValue = Activator.CreateInstance(typeof(T));
			}
	
			rtnValue = Get(index);
			if (rtnValue != null) {
				try {
					rtnValue = AZString.Init(rtnValue).To<T>(defaultValue);
				}
				catch (Exception) {
					rtnValue = defaultValue;
				}
			}
			else {
				rtnValue = defaultValue;
			}
			return (T)rtnValue;
		}
	
		/// <summary>Get 메소드의 generic 형</summary>
		/// <param name="key">string, 반환할 자료의 key 값</param>
		public T Get<T>(string key) {
			object rtnValue;
	
			if (typeof(T).Name.Equals(typeof(String).Name)) {
				rtnValue = "";
			}
			else {
				rtnValue = Activator.CreateInstance(typeof(T));
			}
			rtnValue = Get(key);
			if (rtnValue != null) {
				try {
					rtnValue = AZString.Init(rtnValue).To<T>();
				}
				catch (Exception) { }
			}
			return (T)rtnValue;
		}
	
		/// <summary>Get 메소드의 generic 형</summary>
		/// <param name="key">string, 반환할 자료의 key 값</param>
		/// <param name="defaultValue">T, 예외 발생시 반환받을 기본 값</param>
		public T Get<T>(string key, T defaultValue) {
			object rtnValue;
			if (typeof(T).Name.Equals(typeof(String).Name)) {
				rtnValue = "";
			}
			else {
				rtnValue = Activator.CreateInstance(typeof(T));
			}
	
			rtnValue = Get(key);
			if (rtnValue != null) {
				try {
					rtnValue = AZString.Init(rtnValue).To<T>(defaultValue);
				}
				catch (Exception) {
					rtnValue = defaultValue;
				}
			}
			else {
				rtnValue = defaultValue;
			}
			return (T)rtnValue;
		}
	
		/// <summary>AZData 값을 generic형식의 자료형으로 변환(key값과 일치하는 대상 객체의 property값이 있는 경우 자동 mapping)</summary>
		public T Convert<T>() {
			Type type = typeof(T);
			object rtnValue = Activator.CreateInstance(type);
#if NET_FX
			System.Reflection.PropertyInfo[] properties = type.GetProperties();
			for (int cnti = 0; cnti < properties.Length; cnti++) {
				if (HasKey(properties[cnti].Name)) {
					if (properties[cnti].PropertyType.Name.Equals(typeof(String).Name)) {
						properties[cnti].SetValue(rtnValue, GetString(properties[cnti].Name), null);
					}
					else if (properties[cnti].PropertyType.Name.Equals(typeof(int).Name) || properties[cnti].PropertyType.FullName.Equals(typeof(int?).FullName)) {
						properties[cnti].SetValue(rtnValue, GetInt(properties[cnti].Name), null);
					}
					else if (properties[cnti].PropertyType.Name.Equals(typeof(long).Name) || properties[cnti].PropertyType.FullName.Equals(typeof(long?).FullName)) {
						properties[cnti].SetValue(rtnValue, GetLong(properties[cnti].Name), null);
					}
					else if (properties[cnti].PropertyType.Name.Equals(typeof(float).Name) || properties[cnti].PropertyType.FullName.Equals(typeof(float?).FullName)) {
						properties[cnti].SetValue(rtnValue, GetFloat(properties[cnti].Name), null);
					}
					else if (properties[cnti].PropertyType.Name.Equals(typeof(DateTime).Name) || properties[cnti].PropertyType.FullName.Equals(typeof(DateTime?).FullName)) {
						properties[cnti].SetValue(rtnValue, Get<DateTime>(properties[cnti].Name), null);
					}
					else if (properties[cnti].PropertyType.Name.Equals(typeof(Byte).Name) || properties[cnti].PropertyType.FullName.Equals(typeof(Byte?).FullName)) {
						properties[cnti].SetValue(rtnValue, Get<Byte>(properties[cnti].Name), null);
					}
					else {
						properties[cnti].SetValue(rtnValue, Get(properties[cnti].Name), null);
					}
				}
			}
#endif
#if NET_STD || NET_CORE || NET_STORE
			IEnumerable<PropertyInfo> properties = type.GetRuntimeProperties();
			foreach (PropertyInfo property in properties) {
				if (HasKey(property.Name)) {
					if (property.PropertyType == typeof(string)) {
						property.SetValue(rtnValue, GetString(property.Name), null);
					}
					else if (property.PropertyType == typeof(int)) {
						property.SetValue(rtnValue, GetInt(property.Name), null);
					}
					else if (property.PropertyType == typeof(long)) {
						property.SetValue(rtnValue, GetLong(property.Name), null);
					}
					else if (property.PropertyType == typeof(float)) {
						property.SetValue(rtnValue, GetFloat(property.Name), null);
					}
					else if (property.PropertyType == typeof(DateTime)) {
						property.SetValue(rtnValue, Get<DateTime>(property.Name), null);
					}
					else if (property.PropertyType == typeof(byte)) {
						property.SetValue(rtnValue, Get<Byte>(property.Name), null);
					}
					else {
						object obj = Get(property.Name);
                        Type sourceType = obj.GetType();
                        if (!sourceType.IsArray && property.PropertyType.IsArray) {
                          if (property.PropertyType == typeof(string[])) {
                            property.SetValue(rtnValue, new string[] { GetString(property.Name) }, null);
                          }
                          else if (property.PropertyType == typeof(int[])) {
                            property.SetValue(rtnValue, new int[] { GetInt(property.Name) }, null);
                          }
                          else if (property.PropertyType == typeof(long[])) {
                            property.SetValue(rtnValue, new long[] { GetLong(property.Name) }, null);
                          }
                          else if (property.PropertyType == typeof(float[])) {
                            property.SetValue(rtnValue, new float[] { GetFloat(property.Name) }, null);
                          }
                          else {
                            property.SetValue(rtnValue, Get(property.Name), null);
                          }
                        }
                        else if (sourceType.IsArray && sourceType == typeof(string[]) && property.PropertyType != typeof(string[])) {
                          //
                          string[] srcs = (string[])obj;
                          //
                          if (property.PropertyType == typeof(int[])) {
                            // int[] 의 경우
                            List<int> res = new List<int>();
                            for (int i = 0; i < srcs.Length; i++) {
                              string src = srcs[i];
                              res.Add(src.ToInt(0));
                            }
                            property.SetValue(rtnValue, res.ToArray(), null);
                          }
                          else if (property.PropertyType == typeof(long[])) {
                            // long[] 의 경우
                            List<long> res = new List<long>();
                            for (int i = 0; i < srcs.Length; i++) {
                              string src = srcs[i];
                              res.Add(src.ToLong(0));
                            }
                            property.SetValue(rtnValue, res.ToArray(), null);
                          }
                          else if (property.PropertyType == typeof(float[])) {
                            // float[] 의 경우
                            List<float> res = new List<float>();
                            for (int i = 0; i < srcs.Length; i++) {
                              string src = srcs[i];
                              res.Add(src.ToFloat(0));
                            }
                            property.SetValue(rtnValue, res.ToArray(), null);
                          }
                          else {
                            property.SetValue(rtnValue, obj, null);
                          }
                        }
                        else {
                          property.SetValue(rtnValue, obj, null);
                        }
						// property.SetValue(rtnValue, Get(property.Name), null);
					}
				}
			}
#endif
			return (T)rtnValue;
		}

		/// <summary>Get(int) 메소드에 대한 AZList 캐스팅 처리</summary>
		/// <param name="index">가져올 자료에 대한 index값</param>
		public AZList GetList(int index) { return (AZList)Get(index); }
	
		/// <summary>Get(string) 메소드에 대한 AZList 캐스팅 처리</summary>
		/// <param name="key">가져올 자료에 대한 key값</param>
		public AZList GetList(string key) { return (AZList)Get(key); }
	
		/// <summary>Get(int) 메소드에 대한 AZData 캐스팅 처리</summary>
		/// <param name="index">가져올 자료에 대한 index값</param>
		public AZData GetData(int index) { return (AZData)Get(index); }
	
		/// <summary>Get(string) 메소드에 대한 AZData 캐스팅 처리</summary>
		/// <param name="key">가져올 자료에 대한 key값</param>
		public AZData GetData(string key) { return (AZData)Get(key); }
	
		/// <summary>Get(int) 메소드에 대한 문자열 변경 처리</summary>
		/// <param name="index">가져올 자료에 대한 index값</param>
		public string GetString(int index) { return AZString.Init(Get(index)).String(); }
	
		/// <summary>Get(int, string) 메소드에 대한 문자열 변경 처리</summary>
		/// <param name="index">가져올 자료에 대한 index값</param>
		/// <param name="defaultValue">내부 오류 발생시 반환할 기본값</param>
		public string GetString(int index, string defaultValue) { return AZString.Init(Get(index)).String(defaultValue); }
	
		/// <summary>Get(string) 메소드에 대한 문자열 변경 처리</summary>
		/// <param name="key">가져올 자료에 대한 key값</param>
		public string GetString(string key) { return AZString.Init(Get(key)).String(); }
	
		/// <summary>Get(string) 메소드에 대한 문자열 변경 처리</summary>
		/// <param name="key">가져올 자료에 대한 key값</param>
		/// <param name="defaultValue">내부 오류 발생시 반환할 기본값</param>
		public string GetString(string key, string defaultValue) { return AZString.Init(Get(key)).String(defaultValue); }
	
		/// <summary>Get(int) 메소드에 대한 int형 변경 처리</summary>
		/// <param name="index">가져올 자료에 대한 index값</param>
		public int GetInt(int index) { return AZString.Init(Get(index)).ToInt(); }
	
		/// <summary>Get(int) 메소드에 대한 int형 변경 처리</summary>
		/// <param name="index">가져올 자료에 대한 index값</param>
		/// <param name="defaultValue">내부 오류 발생시 반환할 기본값</param>
		public int GetInt(int index, int defaultValue) { return AZString.Init(Get(index)).ToInt(defaultValue); }
	
		/// <summary>Get(int) 메소드에 대한 int형 변경 처리</summary>
		/// <param name="key">가져올 자료에 대한 key값</param>
		public int GetInt(string key) { return AZString.Init(Get(key)).ToInt(); }
	
		/// <summary>Get(int) 메소드에 대한 int형 변경 처리</summary>
		/// <param name="key">가져올 자료에 대한 key값</param>
		/// <param name="defaultValue">내부 오류 발생시 반환할 기본값</param>
		public int GetInt(string key, int defaultValue) { return AZString.Init(Get(key)).ToInt(defaultValue); }
	
		/// <summary>Get(int) 메소드에 대한 long형 변경 처리</summary>
		/// <param name="index">가져올 자료에 대한 index값</param>
		public long GetLong(int index) { return AZString.Init(Get(index)).ToLong(); }
	
		/// <summary>Get(int) 메소드에 대한 long형 변경 처리</summary>
		/// <param name="index">가져올 자료에 대한 index값</param>
		/// <param name="defaultValue">내부 오류 발생시 반환할 기본값</param>
		public long GetLong(int index, long defaultValue) { return AZString.Init(Get(index)).ToLong(defaultValue); }
	
		/// <summary>Get(int) 메소드에 대한 long형 변경 처리</summary>
		/// <param name="key">가져올 자료에 대한 key값</param>
		public long GetLong(string key) { return AZString.Init(Get(key)).ToLong(); }
	
		/// <summary>Get(int) 메소드에 대한 long형 변경 처리</summary>
		/// <param name="key">가져올 자료에 대한 key값</param>
		/// <param name="defaultValue">내부 오류 발생시 반환할 기본값</param>
		public long GetLong(string key, long defaultValue) { return AZString.Init(Get(key)).ToLong(defaultValue); }
	
		/// <summary>Get(int) 메소드에 대한 float형 변경 처리</summary>
		/// <param name="index">가져올 자료에 대한 index값</param>
		public float GetFloat(int index) { return AZString.Init(Get(index)).ToFloat(); }
	
		/// <summary>Get(int) 메소드에 대한 float형 변경 처리</summary>
		/// <param name="index">가져올 자료에 대한 index값</param>
		/// <param name="defaultValue">내부 오류 발생시 반환할 기본값</param>
		public float GetFloat(int index, float defaultValue) { return AZString.Init(Get(index)).ToFloat(defaultValue); }
	
		/// <summary>Get(int) 메소드에 대한 float형 변경 처리</summary>
		/// <param name="key">가져올 자료에 대한 key값</param>
		public float GetFloat(string key) { return AZString.Init(Get(key)).ToFloat(); }
	
		/// <summary>Get(int) 메소드에 대한 float형 변경 처리</summary>
		/// <param name="key">가져올 자료에 대한 key값</param>
		/// <param name="defaultValue">내부 오류 발생시 반환할 기본값</param>
		public float GetFloat(string key, float defaultValue) { return AZString.Init(Get(key)).ToFloat(defaultValue); }
	
		/// <summary>지정된 index값에 대한 key값 반환</summary>
		/// <param name="index">가져올 자료에 대한 index값</param>
		public string GetKey(int index) { return indexer[index].GetKey(); }
	
		/// <summary>지정된 index값에 대한 link값 반환</summary>
		/// <param name="index">가져올 자료에 대한 index값</param>
		public string GetLink(int index) { return indexer[index].GetLink(); }
	
		/// <summary>key에 해당하는 자료의 값을 지정된 value 값으로 변경. Remove 후 Add 처리를 하게 되며, 이로 인해 index 값이 변경</summary>
		/// <param name="key">string, 변경할 자료의 key값</param>
		/// <param name="value">object, 변경할 자료</param>
		public AZData Set(string key, object value) {
			lock (lockObject) {
				if (map_async.ContainsKey(key)) {
					// 동일 키값이 이미 존재하는 경우
					map_async.Remove(key);
					map_async.Add(key, value);
				}
			}
			return this;
		}

		/// <summary>index에 해당하는 자료의 값을 지정된 value 값으로 변경. Remove 후 Add 처리를 하게 되며, 이로 인해 index 값이 변경</summary>
		/// <param name="index">int, 변경할 자료의 index값, zero base</param>
		/// <param name="value">object, 변경할 자료</param>
		public AZData Set(int index, object value) {
			lock (lockObject) {
				if (index < indexer.Count && map_async[indexer[index].GetLink()] != null) {
					// 동일 키값이 이미 존재하는 경우
					map_async.Remove(indexer[index].GetLink());
					map_async.Add(indexer[index].GetLink(), value);
				}
			}
			return this;
		}
	
		/// <summary>key값에 해당하는 자료를 삭제</summary>
		/// <param name="key">string, 삭제할 자료의 key값</param>
		public AZData Remove(string key) {
			lock (lockObject) {
				if (map_async.ContainsKey(key)) {
					// 동일 키값이 이미 존재하는 경우
					indexer.RemoveAt(IndexOf(key));
					map_async.Remove(key);
				}
			}
			return this;
		}
	
		/// <summary>index값에 해당하는 자료를 삭제</summary>
		/// <param name="index">int, 삭제할 자료의 index값, zero base</param>
		public AZData Remove(int index) {
			lock (lockObject) {
				if (index < indexer.Count) {
					map_async.Remove (indexer [index].GetLink ());
					indexer.RemoveAt (index);
				}
			}
			return this;
		}
	
		/// <summary>key값이 존재하는지 확인</summary>
		/// <param name="key">string, 존재 확인을 원하는 자료의 key값</param>
		public bool HasKey(string key) {
			return map_async.ContainsKey(key);
		}
	
		/// <summary>key값으로 입력된 자료의 index값 반환</summary>
		/// <param name="key">string, key값</param>
		public int IndexOf(string key) {
			int rtnValue = -1;
	
			lock (lockObject) {
				if (map_async.ContainsKey (key)) {
					for (int cnti = 0; cnti < indexer.Count; cnti++) {
						if (indexer [cnti].GetKey ().Equals (key)) {
							rtnValue = cnti;
							break;
						}
					}
				}
			}
			return rtnValue;
		}
	
		/// <summary>현재 자료 갯수를 반환</summary>
		public int Size() { return map_async.Count; }
	
		/// <summary>모든 자료를 삭제처리</summary>
		public void Clear() {
			lock (lockObject) {
				map_async.Clear ();
				indexer.Clear ();
			}
		}
	
		/// <summary>모든 자료를 개별 AZData자료릐 배열로 변환하여 반환</summary>
		public AZData[] ToArray() {
			AZData[] rtnValue = new AZData[Size()];
			for (int cnti = 0; cnti < Size(); cnti++) {
				AZData dmyData = new AZData();
				dmyData.Add(GetKey(cnti), GetString(cnti));
				rtnValue[cnti] = dmyData;
			}
			return rtnValue;
		}
	
		/// <summary>key:value 자료 중 value자료만 문자열로 변환 후 문자열 배열로 반환 처리</summary>
		public string[] ToStringArray() {
			string[] rtnValue = new string[Size()];
			for (int cnti = 0; cnti < Size(); cnti++) {
				rtnValue[cnti] = GetString(cnti);
			}
			return rtnValue;
		}
	
		/// <summary>ToString에 대한 overriding, JSON형식의 {,} 문자 안쪽의 문자열을 생성</summary>
		override public string ToString() {
			StringBuilder builder = new StringBuilder();
				for (int cnti = 0; cnti < indexer.Count; cnti++) {
					try {
						if (Get(cnti) == null) {
							builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + "null");
						}
						else if (Get(cnti).GetType().Equals(typeof(AZData))) {
							builder.Append((cnti > 0 ? ", " : "") + "\"" + GetKey(cnti) + "\"" + ":" + "{" + ((AZData)Get(cnti)).ToString() + "}");
						}
						else if (Get(cnti).GetType().Equals(typeof(AZList))) {
							builder.Append((cnti > 0 ? ", " : "") + "\"" + GetKey(cnti) + "\"" + ":" + "[" + ((AZList)Get(cnti)).ToString() + "]");
						}
						else if (Get(cnti).GetType().Equals(typeof(string[]))) {
							string[] valueSrc = (string[])Get(cnti);
							StringBuilder sBuilder = new StringBuilder();
							for (int cntk=0; cntk<valueSrc.Length; cntk++) {
								if (valueSrc[cntk] == null) {
									sBuilder.AppendFormat("{0}null", cntk > 0 ? "," : "");
								}
								else {
									sBuilder.AppendFormat("{0}\"{1}\"", cntk > 0 ? "," : "", AZString.Encode(AZString.ENCODE.JSON, valueSrc[cntk]));
								}
							}
							builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + "[" + sBuilder.ToString() + "]");
						}
						else if (Get(cnti).GetType().Equals(typeof(int[]))) {
							string valueString = ((int[])Get(cnti)).Join(",");
							builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + "[" + valueString + "]");
						}
						else if (Get(cnti).GetType().Equals(typeof(long[]))) {
							string valueString = ((long[])Get(cnti)).Join(",");
							builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + "[" + valueString + "]");
						}
						else if (Get(cnti).GetType().Equals(typeof(float[]))) {
							string valueString = ((float[])Get(cnti)).Join(",");
							builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + "[" + valueString + "]");
						}
						else if (Get(cnti).GetType().Equals(typeof(double[]))) {
							string valueString = ((double[])Get(cnti)).Join(",");
							builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + "[" + valueString + "]");
						}
						else if (Get(cnti).GetType().Equals(typeof(bool[]))) {
							string valueString = ((bool[])Get(cnti)).Join(",");
							builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + "[" + valueString + "]");
						}
						else {
							string str;
							object value = Get(cnti);
							switch (Type.GetTypeCode(value.GetType())) {
								case TypeCode.Decimal: case TypeCode.Double:
								case TypeCode.Int16: case TypeCode.Int32: case TypeCode.Int64:
								case TypeCode.UInt16: case TypeCode.UInt32: case TypeCode.UInt64:
								case TypeCode.Single:
									str = string.Format(
										"{0}\"{1}\":{2}", (cnti > 0 ? ", " : ""), 
										AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)),
										(Get(cnti) == null ? "" : AZString.Encode(AZString.ENCODE.JSON, value.ToString()))
									);
									break;
								case TypeCode.DBNull:
									str = string.Format(
										"{0}\"{1}\":{2}", (cnti > 0 ? ", " : ""), 
										AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)),
										"null"
									);
									break;
								case TypeCode.Boolean:
									str = string.Format(
										"{0}\"{1}\":{2}", (cnti > 0 ? ", " : ""), 
										AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)),
										((bool)value) ? "true" : "false"
									);
									break;
								case TypeCode.DateTime:
									str = string.Format(
										"{0}\"{1}\":\"{2}\"", (cnti > 0 ? ", " : ""), 
										AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)),
										((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff")
									);
									break;
								default:
									str = string.Format(
										"{0}\"{1}\":\"{2}\"", (cnti > 0 ? ", " : ""), 
										AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)),
										(Get(cnti) == null ? "" : AZString.Encode(AZString.ENCODE.JSON, value.ToString()))
									);
									break;
							}
							builder.Append(str);
						}
					}
					catch (Exception) {
						builder.Append((cnti > 0 ? ", " : "") + "\"" + GetKey(cnti) + "\"" + ":\"\"");
					}
			}
			return builder.ToString();
		}

		/// <summary>json형식의 문자열로 반환</summary>
		public string ToJsonString() {
			return "{" + ToString() + "}";
		}

		/// <summary>xml형식의 문자열로 반환, 정확한 반환을 위해 name, attribute값이 설정되어 있어야 함</summary>
		public string ToXmlString() {
			StringBuilder builder = new StringBuilder();
			builder.Append("<" + Name);
			for (int cnti = 0; cnti < Attribute.Size(); cnti++) {
				if (Attribute.Get(cnti) == null) {
					builder.Append(" " + Attribute.GetKey(cnti));
				}
				else {
					builder.Append(" " + Attribute.GetKey(cnti) + "=\"" + Attribute.Get(cnti) + "\"");
				}
			}
			if (indexer.Count > 0 || Value != null) {
				builder.Append(">");
				for (int cnti = 0; cnti < indexer.Count; cnti++) {
					if (Get(cnti) is AZData) {
						builder.Append(((AZData)Get(cnti)).ToXmlString());
					}
					else if (Get(cnti) is AZList) {
						builder.Append(((AZList)Get(cnti)).ToXmlString());
					}
				}
				builder.Append(Value + "</" + Name + ">");
			}
			else {
				builder.Append(" />");
			}
			return builder.ToString();
		}

		/// <summary>중복된 key값에 대한 처리를 위해 각 키값별로 별도의 link를 생성하여 매치시키기 위한 자료</summary>
		private class KeyLink {
			private string key, link;
			/// <summary>생성자</summary>
			public KeyLink() {
				this.key = "";
				this.link = "";
			}

			/// <summary>생성자</summary>
			public KeyLink(string pKey, string pLink) {
				this.key = pKey;
				this.link = pLink;
			}

			/// <summary>key값 반환</summary>
			public string GetKey() { return this.key; }
			/// <summary>link값 반환</summary>
			public string GetLink() { return this.link; }
			override public string ToString() { return GetKey() + ":" + GetLink(); }
		}

		/// <summary>AttributeData 에서 사용할 key:value 에 대응하는 자료형 클래스</summary>
		private class KeyValue {
			private string key;
			private object value;
			// 기본생성자
			public KeyValue() {
				this.key = "";
				this.value = null;
			}

			public KeyValue(string p_key, object p_value) {
				this.key = p_key;
				this.value = p_value;
			}

			public string GetKey() { return this.key; }
			public object GetValue() { return this.value; }
			public void SetValue(object p_value) { this.value = p_value; }
			override public string ToString() { return GetKey() + ":" + GetValue(); }
		}

		/// <summary>속성값(attribute)에 대한 자료 저장용 클래스</summary>
		public class AttributeData {
			private List<KeyValue> attribute_list;

			public AttributeData() {
				this.attribute_list = new List<KeyValue>();
			}

			public object Add(string p_key, object p_value) {
				this.attribute_list.Add(new KeyValue(p_key, p_value));
				return p_value;
			}

			public object InsertAt(int p_index, string p_key, object p_value) {
				object rtn_value = null;
				if (p_index > -1 && p_index < Size()) {
					this.attribute_list.Insert(p_index, new KeyValue(p_key, p_value));
					rtn_value = p_value;
				}
				return p_value;
			}

			public object InsertBefore(string p_target_key, string p_key, object p_value) {
				object rtn_value = null;
				int index = IndexOf(p_target_key);
				if (index > -1) {
					this.attribute_list.Insert(index, new KeyValue(p_key, p_value));
					rtn_value = p_value;
				}
				return p_value;
			}

			public object InsertAfter(string p_target_key, string p_key, object p_value) {
				object rtn_value = null;
				int index = IndexOf(p_target_key);
				if (index < Size() - 1) {
					this.attribute_list.Insert(index + 1, new KeyValue(p_key, p_value));
					rtn_value = p_value;
				}
				else if (index == Size() - 1) {
					Add(p_key, p_value);
					rtn_value = p_value;
				}
				return p_value;
			}

			public object Remove(string p_key) {
				object rtn_value = null;
				for (int cnti = 0; cnti < this.attribute_list.Count; cnti++) {
					if (this.attribute_list[cnti].GetKey().Equals(p_key)) {
						rtn_value = this.attribute_list[cnti].GetValue();
						this.attribute_list.RemoveAt(cnti);
						break;
					}
				}
				return rtn_value;
			}

			public object Remove(int p_index) {
				object rtn_value = null;
				if (p_index > -1 && p_index < Size()) {
					rtn_value = Get(p_index);
					this.attribute_list.RemoveAt(p_index);
				}
				return rtn_value;
			}

			public object RemoveAt(int p_index) {
				return Remove(p_index);
			}

			public void Clear() {
				this.attribute_list.Clear();
			}

			public int IndexOf(string p_key) {
				int rtn_value = -1;
				for (int cnti = 0; cnti < this.attribute_list.Count; cnti++) {
					if (this.attribute_list[cnti].GetKey().Equals(p_key)) {
						rtn_value = cnti;
						break;
					}
				}
				return rtn_value;
			}

			public object Get(string p_key) {
				object rtn_value = null;
				for (int cnti = 0; cnti < this.attribute_list.Count; cnti++) {
					if (this.attribute_list[cnti].GetKey().Equals(p_key)) {
						rtn_value = this.attribute_list[cnti].GetValue();
						break;
					}
				}
				return rtn_value;
			}

			public object Get(int p_index) {
				object rtn_value = null;
				if (p_index > -1 && p_index < Size()) {
					rtn_value = this.attribute_list[p_index].GetValue();
				}
				return rtn_value;
			}

			public object Set(string p_key, object p_value) {
				object rtn_value = null;
				for (int cnti = 0; cnti < this.attribute_list.Count; cnti++) {
					if (this.attribute_list[cnti].GetKey().Equals(p_key)) {
						rtn_value = this.attribute_list[cnti].GetValue();
						this.attribute_list[cnti].SetValue(p_value);
						break;
					}
				}
				return rtn_value;
			}

			public int Size() {
				return this.attribute_list.Count;
			}

			public string GetKey(int p_index) {
				return this.attribute_list[p_index].GetKey();
			}

			public string[] GetKeys() {
				string[] rtn_value = new string[this.attribute_list.Count];
				for (int cnti = 0; cnti < this.attribute_list.Count; cnti++) {
					rtn_value[cnti] = this.attribute_list[cnti].GetKey();
				}
				return rtn_value;
			}

			/// <summary>ToString 오버라이딩</summary>
			public override string ToString() {
				StringBuilder rtnValue = new StringBuilder();
				for (int cnti = 0; cnti < this.attribute_list.Count; cnti++) {
					string str;
					object value = Get(cnti);
					switch (Type.GetTypeCode(value.GetType())) {
						case TypeCode.Decimal: case TypeCode.Double:
						case TypeCode.Int16: case TypeCode.Int32: case TypeCode.Int64:
						case TypeCode.UInt16: case TypeCode.UInt32: case TypeCode.UInt64:
						case TypeCode.Single:
							str = string.Format(
								"{0}\"{1}\":{2}", (cnti > 0 ? ", " : ""), 
								AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)),
								(Get(cnti) == null ? "" : AZString.Encode(AZString.ENCODE.JSON, value.ToString()))
							);
							break;
						case TypeCode.DBNull:
							str = string.Format(
								"{0}\"{1}\":{2}", (cnti > 0 ? ", " : ""), 
								AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)),
								"null"
							);
							break;
						case TypeCode.Boolean:
							str = string.Format(
								"{0}\"{1}\":{2}", (cnti > 0 ? ", " : ""), 
								AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)),
								((bool)value) ? "true" : "false"
							);
							break;
						default:
							str = string.Format(
								"{0}\"{1}\":\"{2}\"", (cnti > 0 ? ", " : ""), 
								AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)),
								(Get(cnti) == null ? "" : AZString.Encode(AZString.ENCODE.JSON, value.ToString()))
							);
							break;
					}
					rtnValue.Append(str);
				}
				return rtnValue.ToString();
			}

			/// <summary>해당 객체에 대해 JSON 형식의 문자열로 출력</summary>
			public string ToJsonString() {
				return "{" + ToString() + "}";
			}
		}
	}
}