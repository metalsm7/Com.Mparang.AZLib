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

        // IEnumerable 용
        private int index = -1;

        /// Created in 2015-08-13, leeyonghun
		public AZData() {
            if (lockObject == null) {
                lockObject = new object();
            }
			map_async = new Dictionary<string, object>();
			//map_attribute = new Dictionary<string, object>();
            attribute_data = new AttributeData();
			indexer = new List<KeyLink>();
		}

        public AttributeData Attribute {
            get {
                return attribute_data;
            }
        }

        /// <summary>모델 객체로부터 AZData를 생성</summary>
        /// <param name="pSource">AZData로 변경할 모델 객체</param>
        /// Created in 2016-09-21, leeyonghun
        public static AZData From<T>(T pSource) {
            AZData rtnValue = new AZData();
#if NETCOREAPP1_0
            Type type = typeof(T);
            IEnumerable<PropertyInfo> properties = type.GetRuntimeProperties();
            foreach (PropertyInfo property in properties) {
                if (!property.CanRead) { continue; }
                rtnValue.Add(property.Name, property.Name.Equals("SyncRoot") ? property.GetValue(pSource, null).ToString() : property.GetValue(pSource, null));
            }
#endif
#if NET40 || NET452
            Type type = typeof(T);
            System.Reflection.PropertyInfo[] properties = type.GetProperties();
            for (int cnti = 0; cnti < properties.Length; cnti++) {
                System.Reflection.PropertyInfo property = properties[cnti];
                if (!property.CanRead) {
                    continue;
                }
                // ICollection 구현체에 대한 재귀 오류 수정처리, 2016-05-19,, leeyonghun
                rtnValue.Add(property.Name, property.Name.Equals("SyncRoot") ? property.GetValue(pSource, null).ToString() : property.GetValue(pSource, null));
            }
#endif
            return rtnValue;
        }

        /// <summary>json 형식의 문자열을 AZData로 변경, 이 자료를 현재의 자료에 추가</summary>
        /// <param name="pJson">string, json형식의 문자열</param>
        /// Created in 2015-08-13, leeyonghun
        public AZData Add(string pJson) {
            AZData data_json = AZString.JSON.Init(pJson).ToAZData();
            for (int cnti = 0; cnti < data_json.Size(); cnti++) {
                Add(data_json.GetKey(cnti), data_json.Get(cnti));
            }
            return this;
        }

        /// <summary>현재의 자료에 입력받은 자료를 추가</summary>
        /// <param name="pValue">AZData, 추가할 AZData 자료</param>
        /// Created in 2015-08-13, leeyonghun
        public AZData Add(AZData pValue) {
            for (int cnti = 0; cnti < pValue.Size(); cnti++) {
                Add(pValue.GetKey(cnti), pValue.Get(cnti));
            }
            return this;
        }

        /// <summary>key, value 자료를 입력받아 현재의 자료에 추가</summary>
        /// <param name="pKey">string, key값</param>
        /// <param name="pValue">object, kye에 해당하는 자료값</param>
        /// Created in 2015-08-13, leeyonghun
        public AZData Add(string pKey, object pValue) {
            lock (lockObject) {
                if (map_async.ContainsKey(pKey)) {
                    // 동일 키값이 이미 존재하는 경우
                    string linkString = AZString.Random(20);
                    map_async.Add(linkString, pValue);
                    indexer.Add(new KeyLink(pKey, linkString));
                }
                else {
                    map_async.Add(pKey, pValue);
                    indexer.Add(new KeyLink(pKey, pKey));
                }
            }
			return this;
		}

        /// <summary>현재의 자료 중 지정된 순서에 해당하는 자료를 반환</summary>
        /// <param name="pIndex">int, 반환할 자료의 index 값, zero base</param>
        /// Created in 2015-08-13, leeyonghun
		public object Get(int pIndex) {
			if (pIndex >= indexer.Count) {
				return null;
			}
			else {
				return map_async[indexer[pIndex].GetLink()]; 
			}
		}

        /// <summary>현재의 자료 중 key값과 일치하는 자료를 반환</summary>
        /// <param name="pKey">string, 반환할 자료의 key값</param>
        /// Created in 2015-08-13, leeyonghun
		public object Get(string pKey) {
			if (map_async.ContainsKey(pKey)) {
				return map_async[pKey]; 
			}
			else {
				return null;
			}
		}

        /// <summary>현재의 자료 중 지정된 순서에 해당하는 자료를 반환</summary>
        /// <param name="pIndex">int, 반환할 자료의 index 값, zero base</param>
        /// Created in 2015-08-13, leeyonghun
		public object this[int pIndex] {
			get {
				return Get(pIndex);
			}
			set {
				Set(pIndex, value);
			}
		}

        /// <summary>현재의 자료 중 key값과 일치하는 자료를 반환</summary>
        /// <param name="pKey">string, 반환할 자료의 key값</param>
        /// Created in 2015-08-13, leeyonghun
		public object this[string pKey] {
			get {
				return Get(pKey);
			}
			set {
				Set(pKey, value);
			}
		}

        public string Name { get; set; }
        public string Value { get; set; }

        /// Created in 2015-08-13, leeyonghun
        public bool MoveNext() {
            this.index++;
            return (this.index < Size());
        }

        /// Created in 2015-08-13, leeyonghun
        public void Reset() {
            this.index = -1;
        }

        /// Created in 2015-08-13, leeyonghun
        object IEnumerator.Current {
            get {
                return Current;
            }
        }

        /// Created in 2015-08-13, leeyonghun
        IEnumerator IEnumerable.GetEnumerator() {
            return map_async.GetEnumerator();
        }

        /// Created in 2015-08-13, leeyonghun
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
        /// <param name="pIndex">int, 반환할 자료의 index 값, zero base</param>
        /// Created in 2015-07-25, leeyonghun
        public T Get<T>(int pIndex) {
            object rtnValue;
            if (typeof(T).Name.Equals(typeof(String).Name)) {
                rtnValue = "";
            }
            else {
                rtnValue = Activator.CreateInstance(typeof(T));
            }
            rtnValue = Get(pIndex);
            if (rtnValue != null) {
                try {
                    rtnValue = AZString.Init(rtnValue).To<T>();
                }
                catch (Exception) {

                }
            }
            return (T)rtnValue;
        }

        /// <summary>Get 메소드의 generic 형</summary>
        /// <param name="pIndex">int, 반환할 자료의 index 값, zero base</param>
        /// <param name="pDefaultValue">T, 자료 반환시 예외 발생시 반환받을 기본값</param>
        /// Created in 2015-07-25, leeyonghun
        public T Get<T>(int pIndex, T pDefaultValue) {
            object rtnValue;
            if (typeof(T).Name.Equals(typeof(String).Name)) {
                rtnValue = "";
            }
            else {
                rtnValue = Activator.CreateInstance(typeof(T));
            }

            rtnValue = Get(pIndex);
            if (rtnValue != null) {
                try {
                    rtnValue = AZString.Init(rtnValue).To<T>(pDefaultValue);
                }
                catch (Exception) {
                    rtnValue = pDefaultValue;
                }
            }
            else {
                rtnValue = pDefaultValue;
            }
            return (T)rtnValue;
        }

        /// <summary>Get 메소드의 generic 형</summary>
        /// <param name="pKey">string, 반환할 자료의 key 값</param>
        /// Created in 2015-07-25, leeyonghun
        public T Get<T>(string pKey) {
            object rtnValue;

            if (typeof(T).Name.Equals(typeof(String).Name)) {
                rtnValue = "";
            }
            else {
                rtnValue = Activator.CreateInstance(typeof(T));
            }
            rtnValue = Get(pKey);
            if (rtnValue != null) {
                try {
                    rtnValue = AZString.Init(rtnValue).To<T>();
                }
                catch (Exception) {
                }
            }
            return (T)rtnValue;
        }

        /// <summary>Get 메소드의 generic 형</summary>
        /// <param name="pKey">string, 반환할 자료의 key 값</param>
        /// <param name="pDefaultValue">T, 예외 발생시 반환받을 기본 값</param>
        /// Created in 2015-07-25, leeyonghun
        public T Get<T>(string pKey, T pDefaultValue) {
            object rtnValue;
            if (typeof(T).Name.Equals(typeof(String).Name)) {
                rtnValue = "";
            }
            else {
                rtnValue = Activator.CreateInstance(typeof(T));
            }

            rtnValue = Get(pKey);
            if (rtnValue != null) {
                try {
                    rtnValue = AZString.Init(rtnValue).To<T>(pDefaultValue);
                }
                catch (Exception) {
                    rtnValue = pDefaultValue;
                }
            }
            else {
                rtnValue = pDefaultValue;
            }
            return (T)rtnValue;
        }

        /// <summary>AZData 값을 generic형식의 자료형으로 변환(key값과 일치하는 대상 객체의 property값이 있는 경우 자동 mapping)</summary>
        /// Created in 2015-07-25, leeyonghun
        public T Convert<T>() {
            Type type = typeof(T);
            object rtnValue = Activator.CreateInstance(type);
#if NET40 || NET462 || NET452
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
#if NETCOREAPP1_0
            IEnumerable<PropertyInfo> properties = type.GetRuntimeProperties();
            foreach (PropertyInfo property in properties) {
                if (HasKey(property.Name)) {
                    if (property.PropertyType.Name.Equals(typeof(String).Name)) {
                        property.SetValue(rtnValue, GetString(property.Name), null);
                    }
                    else if (property.PropertyType.Name.Equals(typeof(int).Name) || property.PropertyType.FullName.Equals(typeof(int?).FullName)) {
                        property.SetValue(rtnValue, GetInt(property.Name), null);
                    }
                    else if (property.PropertyType.Name.Equals(typeof(long).Name) || property.PropertyType.FullName.Equals(typeof(long?).FullName)) {
                        property.SetValue(rtnValue, GetLong(property.Name), null);
                    }
                    else if (property.PropertyType.Name.Equals(typeof(float).Name) || property.PropertyType.FullName.Equals(typeof(float?).FullName)) {
                        property.SetValue(rtnValue, GetFloat(property.Name), null);
                    }
                    else if (property.PropertyType.Name.Equals(typeof(DateTime).Name) || property.PropertyType.FullName.Equals(typeof(DateTime?).FullName)) {
                        property.SetValue(rtnValue, Get<DateTime>(property.Name), null);
                    }
                    else if (property.PropertyType.Name.Equals(typeof(Byte).Name) || property.PropertyType.FullName.Equals(typeof(Byte?).FullName)) {
                        property.SetValue(rtnValue, Get<Byte>(property.Name), null);
                    }
                    else {
                        property.SetValue(rtnValue, Get(property.Name), null);
                    }
                }
            }
#endif
            return (T)rtnValue;
        }

		public AZList GetList(int pIndex) { return (AZList)Get(pIndex); }

		public AZList GetList(string pKey) { return (AZList)Get(pKey); }

		public AZData GetData(int pIndex) { return (AZData)Get(pIndex); }

        public AZData GetData(string pKey) { return (AZData)Get(pKey); }

        public string GetString(int pIndex) { return AZString.Init(Get(pIndex)).String(); }

        public string GetString(int pIndex, string pDefaultValue) { return AZString.Init(Get(pIndex)).String(pDefaultValue); }

        public string GetString(string pKey) { return AZString.Init(Get(pKey)).String(); }

        public string GetString(string pKey, string pDefaultValue) { return AZString.Init(Get(pKey)).String(pDefaultValue); }

		public int GetInt(int pIndex) { return AZString.Init(Get(pIndex)).ToInt(); }

		public int GetInt(int pIndex, int pDefaultValue) { return AZString.Init(Get(pIndex)).ToInt(pDefaultValue); }

		public int GetInt(string pKey) { return AZString.Init(Get(pKey)).ToInt(); }

        public int GetInt(string pKey, int pDefaultValue) { return AZString.Init(Get(pKey)).ToInt(pDefaultValue); }

        public long GetLong(int pIndex) { return AZString.Init(Get(pIndex)).ToLong(); }

        public long GetLong(int pIndex, long pDefaultValue) { return AZString.Init(Get(pIndex)).ToLong(pDefaultValue); }

        public long GetLong(string pKey) { return AZString.Init(Get(pKey)).ToLong(); }

        public long GetLong(string pKey, long pDefaultValue) { return AZString.Init(Get(pKey)).ToLong(pDefaultValue); }

        public float GetFloat(int pIndex) { return AZString.Init(Get(pIndex)).ToFloat(); }

        public float GetFloat(int pIndex, float pDefaultValue) { return AZString.Init(Get(pIndex)).ToFloat(pDefaultValue); }

        public float GetFloat(string pKey) { return AZString.Init(Get(pKey)).ToFloat(); }

        public float GetFloat(string pKey, float pDefaultValue) { return AZString.Init(Get(pKey)).ToFloat(pDefaultValue); }

		public string GetKey(int pIndex) { return indexer[pIndex].GetKey(); }

		public string GetLink(int pIndex) { return indexer[pIndex].GetLink(); }

        /// <summary>key에 해당하는 자료의 값을 지정된 value 값으로 변경. Remove 후 Add 처리를 하게 되며, 이로 인해 index 값이 변경</summary>
        /// <param name="pKey">string, 변경할 자료의 key값</param>
        /// <param name="pValue">object, 변경할 자료</param>
        /// Created in 2017-08-17, leeyonghun
		public AZData Set(string pKey, object pValue) {
			//bool rtnValue;
			lock (lockObject) {
				//rtnValue = false;
				if (map_async.ContainsKey(pKey)) {
					// 동일 키값이 이미 존재하는 경우
					map_async.Remove(pKey);
					map_async.Add(pKey, pValue);
					//rtnValue = true;
				}
			}
			return this;
		}

        /// <summary>index에 해당하는 자료의 값을 지정된 value 값으로 변경. Remove 후 Add 처리를 하게 되며, 이로 인해 index 값이 변경</summary>
        /// <param name="pIndex">int, 변경할 자료의 index값, zero base</param>
        /// <param name="pValue">object, 변경할 자료</param>
        /// Created in 2017-08-17, leeyonghun
		public AZData Set(int pIndex, object pValue) {
			//bool rtnValue;
			lock (lockObject) {
				//rtnValue = false;
				if (pIndex < indexer.Count) {
					if (map_async[indexer[pIndex].GetLink()] != null) {
						// 동일 키값이 이미 존재하는 경우
						map_async.Remove(indexer[pIndex].GetLink());
						map_async.Add(indexer[pIndex].GetLink(), pValue);
						//rtnValue = true;
					}
				}
			}
			return this;
		}

        /// <summary>key값에 해당하는 자료를 삭제</summary>
        /// <param name="pKey">string, 삭제할 자료의 key값</param>
        /// Created in 2017-08-17, leeyonghun
		public AZData Remove(string pKey) {
			//bool rtnValue;
			lock (lockObject) {
				//rtnValue = false;
				if (map_async.ContainsKey(pKey)) {
					// 동일 키값이 이미 존재하는 경우
					indexer.RemoveAt(IndexOf(pKey));
					map_async.Remove(pKey);
					//rtnValue = true;
				}
			}
			return this;
		}

        /// <summary>index값에 해당하는 자료를 삭제</summary>
        /// <param name="pIndex">int, 삭제할 자료의 index값, zero base</param>
        /// Created in 2017-08-17, leeyonghun
        public AZData Remove(int pIndex) {
			//bool rtnValue;
			lock (lockObject) {
				//rtnValue = false;
				if (pIndex < indexer.Count) {
					map_async.Remove (indexer [pIndex].GetLink ());
					indexer.RemoveAt (pIndex);
					//rtnValue = true;
				}
			}
			return this;
		}

        /// <summary>key값이 존재하는지 확인</summary>
        /// <param name="p_key">string, 존재 확인을 원하는 자료의 key값</param>
        /// Created in 2017-08-17, leeyonghun
        public bool HasKey(string p_key) {
            return map_async.ContainsKey(p_key);
        }

        /// <summary>key값으로 입력된 자료의 index값 반환</summary>
        /// <param name="pKey">string, key값</param>
        /// Created in 2017-08-17, leeyonghun
		public int IndexOf(string pKey) {
			int rtnValue = -1;

			lock (lockObject) {
				if (map_async.ContainsKey (pKey)) {
					for (int cnti = 0; cnti < indexer.Count; cnti++) {
						if (indexer [cnti].GetKey ().Equals (pKey)) {
							rtnValue = cnti;
							break;
						}
					}
				}
			}

			return rtnValue;
		}

        /// <summary>현재 자료 갯수를 반환</summary>
        /// Created in 2017-08-17, leeyonghun
		public int Size() { return map_async.Count; }

        /// <summary>모든 자료를 삭제처리</summary>
        /// Created in 2017-08-17, leeyonghun
		public void Clear() {
			lock (lockObject) {
				map_async.Clear ();
				indexer.Clear ();
			}
		}

        /// <summary>모든 자료를 개별 AZData자료릐 배열로 변환하여 반환</summary>
        /// Created in 2017-08-17, leeyonghun
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
        /// Created in 2017-08-17, leeyonghun
		public string[] ToStringArray() {
			string[] rtnValue = new string[Size()];
			for (int cnti = 0; cnti < Size(); cnti++) {
				rtnValue[cnti] = GetString(cnti);
			}
			return rtnValue;
		}

		override public string ToString() {
			StringBuilder builder = new StringBuilder();
            for (int cnti = 0; cnti < indexer.Count; cnti++) {
                try {
                    if (Get(cnti).GetType().Equals(typeof(AZData))) {
                        builder.Append((cnti > 0 ? ", " : "") + "\"" + GetKey(cnti) + "\"" + ":" + "{" + ((AZData)Get(cnti)).ToString() + "}");
                    }
                    else if (Get(cnti).GetType().Equals(typeof(AZList))) {
                        builder.Append((cnti > 0 ? ", " : "") + "\"" + GetKey(cnti) + "\"" + ":" + "[" + ((AZList)Get(cnti)).ToString() + "]");
                    }
                    else if (Get(cnti).GetType().Equals(typeof(string))) {
                        string valueString = GetString(cnti);
                        builder.Append((cnti > 0 ? ", " : "") + "\"" + GetKey(cnti) + "\"" + ":" + "\"" + valueString + "\"");
                    }
                    else {
                        string valueString = "";
                        if (!Get(cnti).GetType().IsNested || Get(cnti).GetType() == typeof(DBNull)) {
                            valueString = GetString(cnti);
                        }
                        else {
                            valueString = Get(cnti).ToString();
                        }
                        builder.Append((cnti > 0 ? ", " : "") + "\"" + GetKey(cnti) + "\"" + ":" + "\"" + valueString + "\"");
                    }
                }
                catch (Exception) {
                    builder.Append((cnti > 0 ? ", " : "") + "\"" + GetKey(cnti) + "\"" + ":\"\"");
                }
                /*
				if (Get(cnti) is AZData) {
					builder.Append((cnti > 0 ? ", " : "") + "\"" + GetKey(cnti) + "\"" + 
						":" + "{" + ((AZData)Get(cnti)).ToString() + "}");
				}
				else if (Get(cnti) is AZList) {
					builder.Append((cnti > 0 ? ", " : "") + "\"" + GetKey(cnti) + "\"" + 
						":" + "[" + ((AZList)Get(cnti)).ToString() + "]");
				}
				else {
					string valueString = GetString(cnti);
					//builder.Append((cnti > 0 ? ", " : "") + GetKey(cnti) + ":" + (valueString.Contains(" ") ? "\"" + valueString + "\"" : valueString));
					builder.Append((cnti > 0 ? ", " : "") + "\"" + GetKey(cnti) + "\"" + 
						":" + "\"" + valueString + "\"");
				}
                */
			}
			return builder.ToString();
		}

        /// <summary>json형식의 문자열로 반환</summary>
        /// <returns>string, json형식의 문자열</returns>
        /// Created in 2017-08-17, leeyonghun
		public string ToJsonString() {
			StringBuilder builder = new StringBuilder();
            for (int cnti = 0; cnti < indexer.Count; cnti++) {
                try {
                    if (Get(cnti).GetType().Equals(typeof(AZData))) {
                        builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + ((AZData)Get(cnti)).ToJsonString());
                    }
                    else if (Get(cnti).GetType().Equals(typeof(AZList))) {
                        builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + ((AZList)Get(cnti)).ToJsonString());
                    }
                    else if (Get(cnti).GetType().Equals(typeof(string))) {
                        string valueString = GetString(cnti);
                        builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + "\"" + AZString.Encode(AZString.ENCODE.JSON, valueString) + "\"");
                    }
                    else if (Get(cnti).GetType().Equals(typeof(string[]))) {
                        string valueString = ((string[])Get(cnti)).Each(x => x.Encode(AZString.ENCODE.JSON)).Join(",");
                        builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + "[" + AZString.Encode(AZString.ENCODE.JSON, valueString) + "]");
                    }
                    else if (Get(cnti).GetType().Equals(typeof(int[])) || Get(cnti).GetType().Equals(typeof(double[]))) {
                        string valueString = ((int[])Get(cnti)).Join(",");
                        builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + "[" + AZString.Encode(AZString.ENCODE.JSON, valueString) + "]");
                    }
                    else {
                        string valueString = "";
                        if (!Get(cnti).GetType().IsNested || Get(cnti).GetType() == typeof(DBNull)) {
                            valueString = GetString(cnti);
                            builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + "\"" + AZString.Encode(AZString.ENCODE.JSON, valueString) + "\"");
                        }
                        else {
                            valueString = Get(cnti).ToString();
                            builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":" + valueString);
                        }
                    }
                }
                catch (Exception) {
                    builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" + ":\"\"");
                }
                /*
				if (Get(cnti) is AZData) {
					builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.ToJSONSafeEncoding(GetKey(cnti)) + "\"" + 
						":" + ((AZData)Get(cnti)).ToJsonString());
				}
				else if (Get(cnti) is AZList) {
					builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.ToJSONSafeEncoding(GetKey(cnti)) + "\"" + 
						":" + ((AZList)Get(cnti)).ToJsonString());
				}
				else {
					string valueString = GetString(cnti);
					//builder.Append((cnti > 0 ? ", " : "") + GetKey(cnti) + ":" + (valueString.Contains(" ") ? "\"" + valueString + "\"" : valueString));
					builder.Append((cnti > 0 ? ", " : "") + "\"" + AZString.ToJSONSafeEncoding(GetKey(cnti)) + "\"" + 
						":" + "\"" + AZString.ToJSONSafeEncoding(valueString) + "\"");
				}
                */
			}
			return "{" + builder.ToString() + "}";
		}

        public string ToXmlString() {
            StringBuilder builder = new StringBuilder();
            builder.Append("<" + Name);
            //string[] attribute_names = GetAttributeNames();
            for (int cnti = 0; cnti < Attribute.Size(); cnti++) {
                //builder.Append(" " + attribute_names[cnti] + "=\"" + GetAttribute(attribute_names[cnti]) + "\"");
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

        private class KeyLink {
            private string key, link;
            // 기본생성자
            public KeyLink() {
                this.key = "";
                this.link = "";
            }

            public KeyLink(string pKey, string pLink) {
                this.key = pKey;
                this.link = pLink;
            }

            public string GetKey() { return this.key; }
            public string GetLink() { return this.link; }
            override public string ToString() { return GetKey() + ":" + GetLink(); }
        }

        /// <summary>AttributeData 에서 사용할 key:value 에 대응하는 자료형 클래스</summary>
        /// Created in 2015-05-22, leeyonghun
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
        /// Created in 2015-05-22, leeyonghun
        public class AttributeData {
            private List<KeyValue> attribute_list;

            /// Created in 2015-05-22, leeyonghun
            public AttributeData() {
                this.attribute_list = new List<KeyValue>();
            }

            /// Created in 2015-05-22, leeyonghun
            public object Add(string p_key, object p_value) {
                this.attribute_list.Add(new KeyValue(p_key, p_value));
                return p_value;
            }

            /// Created in 2015-05-22, leeyonghun
            public object InsertAt(int p_index, string p_key, object p_value) {
                object rtn_value = null;
                if (p_index > -1 && p_index < Size()) {
                    this.attribute_list.Insert(p_index, new KeyValue(p_key, p_value));
                    rtn_value = p_value;
                }
                return p_value;
            }

            /// Created in 2015-05-22, leeyonghun
            public object InsertBefore(string p_target_key, string p_key, object p_value) {
                object rtn_value = null;
                int index = IndexOf(p_target_key);
                if (index > -1) {
                    this.attribute_list.Insert(index, new KeyValue(p_key, p_value));
                    rtn_value = p_value;
                }
                return p_value;
            }

            /// Created in 2015-05-22, leeyonghun
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

            /// Created in 2015-05-22, leeyonghun
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

            /// Created in 2015-05-22, leeyonghun
            public object Remove(int p_index) {
                object rtn_value = null;
                if (p_index > -1 && p_index < Size()) {
                    rtn_value = Get(p_index);
                    this.attribute_list.RemoveAt(p_index);
                }
                return rtn_value;
            }

            /// Created in 2015-05-22, leeyonghun
            public object RemoveAt(int p_index) {
                return Remove(p_index);
            }

            /// Created in 2015-05-22, leeyonghun
            public void Clear() {
                this.attribute_list.Clear();
            }

            /// Created in 2015-05-22, leeyonghun
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

            /// Created in 2015-05-22, leeyonghun
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

            /// Created in 2015-05-22, leeyonghun
            public object Get(int p_index) {
                object rtn_value = null;
                if (p_index > -1 && p_index < Size()) {
                    rtn_value = this.attribute_list[p_index].GetValue();
                }
                return rtn_value;
            }

            /// Created in 2015-05-22, leeyonghun
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

            /// Created in 2015-05-22, leeyonghun
            public int Size() {
                return this.attribute_list.Count;
            }

            /// Created in 2015-05-22, leeyonghun
            public string GetKey(int p_index) {
                return this.attribute_list[p_index].GetKey();
            }

            /// Created in 2015-05-22, leeyonghun
            public string[] GetKeys() {
                string[] rtn_value = new string[this.attribute_list.Count];
                for (int cnti = 0; cnti < this.attribute_list.Count; cnti++) {
                    rtn_value[cnti] = this.attribute_list[cnti].GetKey();
                }
                return rtn_value;
            }

            /// <summary>ToString 오버라이딩</summary>
            /// Created in 2015-05-27, leeyonghun
            public override string ToString() {
                StringBuilder rtnValue = new StringBuilder();
                for (int cnti = 0; cnti < this.attribute_list.Count; cnti++) {
                    rtnValue.Append((cnti > 0 ? ", " : "") + "\"" + AZString.Encode(AZString.ENCODE.JSON, GetKey(cnti)) + "\"" +
                        ":" + "\"" + (Get(cnti) == null ? "" : AZString.Encode(AZString.ENCODE.JSON, Get(cnti).ToString())) + "\"");
                }
                return rtnValue.ToString();
            }

            /// <summary>해당 객체에 대해 JSON 형식의 문자열로 출력</summary>
            /// Created in 2015-05-27, leeyonghun
            public string ToJsonString() {
                return "{" + ToString() + "}";
            }
        }
	}
}