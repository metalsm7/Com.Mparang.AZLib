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

    /// <summary>기본 생성자</summary>
    /// Created in 2017-08-17, leeyonghun
		public AZList() {
      list = new List<AZData>();
      //map_attribute = new Dictionary<string, object>();
      attribute_data = new AttributeData();
		}

    /// Created in 2015-08-13, leeyonghun
    public AttributeData Attribute {
      get {
        return attribute_data;
      }
    }

    /// <summary>AZData 자료를 추가</summary>
    /// <param name="pData">AZData, 추가할 AZData 자료</param>
    /// Created in 2015-08-13, leeyonghun
		public AZList Add(AZData pData) { list.Add(pData); return this; }

    /// <summary>AZList 자료의 모든 AZData 자료를 추가</summary>
    /// <param name="pList">AZList, 추가할 AZList 자료</param>
    /// Created in 2015-08-13, leeyonghun
    public AZList Add(AZList pList) {
      for (int cnti = 0; cnti < pList.Size(); cnti++) {
        this.Add(pList.Get(cnti));
      }
      return this;
    }

    /// <summary>일치하는 AZData 자료를 현재 자료에서 삭제</summary>
    /// <param name="pData">AZData, 삭제할 AZData 자료</param>
    /// Created in 2015-08-13, leeyonghun
    public AZList Remove(AZData pData) { list.Remove(pData); return this; }

    /// <summary>현재 자료에서 선택된 index 값에 해당하는 자료 삭제</summary>
    /// <param name="pIndex">int, 삭제할 AZData 자료가 위치하는 index 값, zero base</param>
    /// Created in 2015-08-13, leeyonghun
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

    /// <summart>모든 자료를 삭제</summary>
    /// Created in 2015-08-13, leeyonghun
		public void Clear() { list.Clear(); }

    /// <summary>현재 자료의 갯수 반환</summary>
    /// Created in 2015-08-13, leeyonghun
		public int Size() { return list.Count; }

    public string Name { get; set; }

    /// <summary>현재 자료 중 key값과 일치하는 자료가 있는지 확인</summary>
    /// <param name="key">string, 일치하는 key값이 있는지 확인을 원하는 key 값</param>
    /// Created in 2017-06-29, leeyonghun
    public int IndexOf(string key) {
      int rtn_value = -1;
      for (int cnti=0; cnti<Size(); cnti++) {
        if (Get(cnti).HasKey(key)) {
          rtn_value = cnti;
          break;
        }
      }
      return rtn_value;
    }

    /// <summary>현재 자료 중 key값과 value값이 일치하는 자료가 있는지 확인</summary>
    /// <param name="key">string, 비교할 key값</param>
    /// <param name="value">object, 비교할 value값</param>
    /// Created in 2017-06-29, leeyonghun
    public int IndexOf(string key, object value) {
      int rtn_value = -1;
      for (int cnti=0; cnti<Size(); cnti++) {
        if (Get(cnti).HasKey(key) && Get(cnti).Get(key).Equals(value)) {
          rtn_value = cnti;
          break;
        }
      }
      return rtn_value;
    }

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

    /// Created in 2015-08-13, leeyonghun
    void IDisposable.Dispose() { }

    /// Created in 2015-08-13, leeyonghun
    IEnumerator IEnumerable.GetEnumerator() {
      //return (IEnumerator)GetEnumerator();
      return this.list.GetEnumerator();
    }

    /// Created in 2015-08-13, leeyonghun
    public IEnumerator<AZData> GetEnumerator() {
      return this.list.GetEnumerator();
    }

    /// Created in 2015-08-13, leeyonghun
		public override string ToString() {
			StringBuilder builder = new StringBuilder();
			for (int cnti = 0; cnti < list.Count; cnti++) {
				AZData data = list[cnti];
				builder.Append((cnti > 0 ? ", " : "") + "{" + data.ToString() + "}");
			}
			return builder.ToString ();
		}

        /// Created in 2015-08-13, leeyonghun
		public string ToJsonString() {
			StringBuilder builder = new StringBuilder();
			for (int cnti = 0; cnti < list.Count; cnti++) {
				AZData data = list[cnti];
				builder.Append((cnti > 0 ? ", " : "") + data.ToJsonString());
			}
			return "[" + builder.ToString() + "]";
		}

    /// Created in 2015-08-13, leeyonghun
    public string ToXmlString() {
      StringBuilder builder = new StringBuilder();
      for (int cnti = 0; cnti < Size(); cnti++) {
        AZData data = list[cnti];
        builder.Append(data.ToXmlString());
      }
      return builder.ToString();
    }

    /// Created in 2015-08-13, leeyonghun
    public AZData Get(int pIndex) { return list[pIndex]; }

    /// <summary>모든 AZData 중 Name값이 설정되고, Name값이 지정된 Name값과 일치하는 자료의 목록을 반환</summary>
    /// <param name="pName">string, </param>
    /// Created in 2015-08-13, leeyonghun
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

    /// <summary>index에 해당하는 AZData 자료 반환</summary>
    /// <param name="pIndex">int, 반환할 자료의 index 값, zero base</param>
    /// Created in 2015-08-13, leeyonghun
		public AZData this[int pIndex] {
			get {
				return Get(pIndex);
			}
		}

    /// <summary>모든 AZData 중 Name값이 설정되고, Name값이 지정된 Name값과 일치하는 자료의 목록을 반환</summary>
    /// <param name="pName">string, </param>
    /// Created in 2015-08-13, leeyonghun
    public AZList this[string pName] {
      get {
        return Get(pName);
      }
    }

    /// <summary>모든 AZData자료에 대해 개별로 T형식으로 Convert()를 수행 후 T형식의 배열로 반환</summary>
    /// Created in 2015-07-25, leeyonghun
    public T[] Convert<T>() {
      T[] rtnValue = new T[Size()];
      for (int cnti = 0; cnti < Size(); cnti++) {
        rtnValue[cnti] = Get(cnti).Convert<T>();
      }
      return rtnValue;
    }

    /// AttributeData 에서 사용할 key:value 에 대응하는 자료형 클래스
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

    /// 속성값(attribute)에 대한 자료 저장용 클래스
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
        if (p_index > -1 || p_index < Size()) {
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
        if (p_index > -1 || p_index < Size()) {
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
        for (int cnti = 0; cnti < attribute_list.Count; cnti++) {
          rtn_value[cnti] = attribute_list[cnti].GetKey();
        }
        return rtn_value;
      }
    }
	}
}