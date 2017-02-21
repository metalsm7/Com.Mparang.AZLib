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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Com.Mparang.AZLib {
    //[Serializable()]
	public class AZList : IEnumerator, IEnumerator<AZData>, IEnumerable, IEnumerable<AZData> {
        private List<AZData> list = null;
        //private Dictionary<string, object> map_attribute = null;
        private AttributeData attribute_data = null;

        // IEnumerable 용
        private int index = -1;

		public AZList() {
            list = new List<AZData>();
            //map_attribute = new Dictionary<string, object>();
            attribute_data = new AttributeData();
		}

        public AttributeData Attribute {
            get {
                return attribute_data;
            }
        }

		public AZList Add(AZData pData) { list.Add(pData); return this; }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        public AZList Add(AZList pList) {
            for (int cnti = 0; cnti < pList.Size(); cnti++) {
                this.Add(pList.Get(cnti));
            }
            return this;
        }

        public AZList Remove(AZData pData) { list.Remove(pData); return this; }

		public AZList Remove(int pIndex) {
			//AZData rtnValue = null;
			if (list.Count > pIndex) {
				if (list[pIndex] != null) {
					//rtnValue = list[pIndex];
					list.RemoveAt(pIndex);
				}
			}
			//return rtnValue;
            return this;
		}

		public void Clear() { list.Clear(); }

		public int Size() { return list.Count; }

        public string Name { get; set; }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        public bool MoveNext() {
            this.index++;
            return (this.index < Size());
        }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        public void Reset() {
            this.index = -1;
        }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        object IEnumerator.Current {
            get {
                return Current;
            }
        }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        public AZData Current {
            get {
                try {
                    return Get(this.index);
                }
                catch (IndexOutOfRangeException) {
                    throw new InvalidOperationException();
                }
            }
        }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        void IDisposable.Dispose() { }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        IEnumerator IEnumerable.GetEnumerator() {
            //return (IEnumerator)GetEnumerator();
            return this.list.GetEnumerator();
        }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        public IEnumerator<AZData> GetEnumerator() {
            return this.list.GetEnumerator();
        }

		public override string ToString() {
			StringBuilder builder = new StringBuilder();
			for (int cnti = 0; cnti < list.Count; cnti++) {
				AZData data = list[cnti];
				builder.Append((cnti > 0 ? ", " : "") + "{" + data.ToString() + "}");
			}
			return builder.ToString ();
		}

		public string ToJsonString() {
			StringBuilder builder = new StringBuilder();
			for (int cnti = 0; cnti < list.Count; cnti++) {
				AZData data = list[cnti];
				builder.Append((cnti > 0 ? ", " : "") + data.ToJsonString());
			}
			return "[" + builder.ToString() + "]";
		}

        public string ToXmlString() {
            StringBuilder builder = new StringBuilder();
            for (int cnti = 0; cnti < Size(); cnti++) {
                AZData data = list[cnti];
                builder.Append(data.ToXmlString());
            }
            return builder.ToString();
        }

        public AZData Get(int pIndex) { return list[pIndex]; }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        public AZList Get(string pName) {
            AZList rtnValue = new AZList();
            for (int cnti = 0; cnti < this.list.Count; cnti++) {
                if (Get(cnti).Name != null && Get(cnti).Name.Equals(pName)) {
                    rtnValue.Add(Get(cnti));
                }
            }
            return rtnValue;
        }

		//public AZData GetData(int pIndex) { return list[pIndex]; }

		public AZData this[int pIndex] {
			get {
				return Get(pIndex);
			}
		}

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        public AZList this[string pName] {
            get {
                return Get(pName);
            }
        }

        /**
         * <summary></summary>
         * Created in 2015-07-25, leeyonghun
         */
        public T[] Convert<T>() {
            T[] rtnValue = new T[Size()];
            for (int cnti = 0; cnti < Size(); cnti++) {
                rtnValue[cnti] = Get(cnti).Convert<T>();
            }
            return rtnValue;
        }

        /**
         * AttributeData 에서 사용할 key:value 에 대응하는 자료형 클래스
         * 작성일 : 2015-05-22 이용훈
         */
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

        /**
         * 속성값(attribute)에 대한 자료 저장용 클래스
         * 작성일 : 2015-05-22 이용훈
         */
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
                if (p_index > -1 || p_index < Size()) {
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
                if (p_index > -1 || p_index < Size()) {
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
                for (int cnti = 0; cnti < attribute_list.Count; cnti++) {
                    rtn_value[cnti] = attribute_list[cnti].GetKey();
                }
                return rtn_value;
            }
        }
	}
}