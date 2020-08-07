using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public AZList() {
      list = new List<AZData>();
      attribute_data = new AttributeData();
    }
    
    /// <summary>모델 객체로부터 AZData를 생성</summary>
    /// <param name="source">AZList로 변경할 모델 객체</param>
    public static AZList Parse(string source) {
      return AZString.JSON.ToAZList(source);
    }

    public AttributeData Attribute {
      get { return attribute_data; }
    }

    /// <summary>AZData 자료를 추가</summary>
    /// <param name="data">AZData, 추가할 AZData 자료</param>
    public AZList Add(AZData data) {
      list.Add(data); return this;
    }

    /// <summary>AZList 자료의 모든 AZData 자료를 추가</summary>
    /// <param name="list">AZList, 추가할 AZList 자료</param>
    public AZList Add(AZList list) {
      for (int cnti = 0; cnti < list.Size(); cnti++) {
        this.Add(list.Get(cnti));
      }
      return this;
    }

    public AZData Push(AZData data) {
      Add(data);
      return data;
    }

    /// <summary>일치하는 AZData 자료를 현재 자료에서 삭제</summary>
    /// <param name="data">AZData, 삭제할 AZData 자료</param>
    public AZList Remove(AZData data) { list.Remove(data); return this; }

    /// <summary>현재 자료에서 선택된 index 값에 해당하는 자료 삭제</summary>
    /// <param name="index">int, 삭제할 AZData 자료가 위치하는 index 값, zero base</param>
    public AZList Remove(int index) {
      if (list.Count > index && list[index] != null) list.RemoveAt(index);
      return this;
    }

    public AZData Pop() {
      if (Size() < 1) return null;
      AZData rtnVal = Get(Size() - 1);
      Remove(Size() - 1);
      return rtnVal;
    }

    public AZData Shift() {
      if (Size() < 1) return null;
      AZData rtnVal = Get(0);
      Remove(0);
      return rtnVal;
    }

    public AZData Unshift(AZData data) {
      list.Insert(0, data);
      return data;
    }

    public AZList Splice(int idx, int length) {
      AZList rtnVal = new AZList();
      //
      if (Size() < 1 || length < 1 || idx < 0) return rtnVal;
      int sIdx = idx;
      int eIdx = idx + length <= Size() ? idx + length : Size();
      for (int i = sIdx; i < eIdx; i++) {
        if (sIdx < 0 || sIdx >= Size()) continue;
        rtnVal.Add(Get(sIdx));
        Remove(sIdx);
      }
      //
      return rtnVal;
    }
    
    /// <summary>
    /// 지정된 key를 기준으로 중복되는 자료를 삭제 합니다.
    /// getFirst 값에 따라 true인 경우 처음값을, false인 경우 마지막값을 반환합니다.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="getFirst"></param>
    /// <returns></returns>
    public AZList UniqBy(string key, bool getFirst = true) {
      return this.GroupBy(row => row.GetLong(key)).Select(row => getFirst ? row.FirstOrDefault() : row.LastOrDefault()).OrderBy(row => row.GetLong(key)).ToAZList();
    }

    /// <summary>
    /// 지정된 key와 일치하는 자료에 대해 입력된 자료를 추가 합니다.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="key"></param>
    /// <param name="overwrite"></param>
    /// <returns></returns>
    public AZList Merge(AZList list, string key, bool overwrite = false) {
      var rtnVal = 
        from src in this
        from tgt in list 
          .Where(row => row.Get(key).Equals(src.Get(key)))
          .DefaultIfEmpty()
        select tgt == null ? src : src.Merge(tgt, overwrite);
      return rtnVal.ToAZList();
      /*
      for (int i=0; i<Size(); i++) {
        AZData src = Get(i);
        if (src.HasKey(key)) {
          for (int k=0; k<list.Size(); k++) {
            AZData tgt = list.Get(k);
            if (tgt.HasKey(key)) {
              if (src.Get(key) == tgt.Get(key)) {
                src.Merge(tgt, overwrite);
                break;
              }
            }
          }
        }
      }
      return this;
      */
    }
    
    /// <summart>모든 자료를 삭제</summary>
    public void Clear() { list.Clear(); }

    /// <summary>현재 자료의 갯수 반환</summary>
    public int Size() { return list.Count; }

    public string Name { get; set; }

    /// <summary>현재 자료 중 key값과 일치하는 자료가 있는지 확인</summary>
    /// <param name="key">string, 일치하는 key값이 있는지 확인을 원하는 key 값</param>
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

    /// <summary></summary>
    public bool MoveNext() {
      this.index++;
      return (this.index < Size());
    }

    /// <summary></summary>
    public void Reset() {
      this.index = -1;
    }

    /// <summary></summary>
    object IEnumerator.Current {
      get { return Current; }
    }

    /// <summary></summary>
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

    /// <summary></summary>
    void IDisposable.Dispose() { }

    /// <summary></summary>
    IEnumerator IEnumerable.GetEnumerator() {
      return this.list.GetEnumerator();
    }
    
    /// <summary></summary>
    public IEnumerator<AZData> GetEnumerator() {
      return this.list.GetEnumerator();
    }

    /// <summary></summary>
    public override string ToString() {
      StringBuilder builder = new StringBuilder();
      for (int cnti = 0; cnti < list.Count; cnti++) {
        AZData data = list[cnti];
        builder.Append((cnti > 0 ? ", " : "") + "{" + data.ToString() + "}");
      }
      return builder.ToString ();
    }

    /// <summary></summary>
    public string ToJsonString() {
      StringBuilder builder = new StringBuilder();
      for (int cnti = 0; cnti < list.Count; cnti++) {
        AZData data = list[cnti];
        builder.Append((cnti > 0 ? ", " : "") + data.ToJsonString());
      }
      return "[" + builder.ToString() + "]";
    }

    /// <summary></summary>
    public string ToXmlString() {
      StringBuilder builder = new StringBuilder();
      for (int cnti = 0; cnti < Size(); cnti++) {
        AZData data = list[cnti];
        builder.Append(data.ToXmlString());
      }
      return builder.ToString();
    }

    /// <summary></summary>
    public AZData Get(int index) { return list[index]; }

    /// <summary>모든 AZData 중 Name값이 설정되고, Name값이 지정된 Name값과 일치하는 자료의 목록을 반환</summary>
    /// <param name="name">string, </param>
    public AZList Get(string name) {
      AZList rtnValue = new AZList();
      for (int cnti = 0; cnti < this.list.Count; cnti++) {
        if (Get(cnti).Name != null && Get(cnti).Name.Equals(name)) {
          rtnValue.Add(Get(cnti));
        }
      }
      return rtnValue;
    }

    /// <summary>index에 해당하는 AZData 자료 반환</summary>
    /// <param name="index">int, 반환할 자료의 index 값, zero base</param>
    public AZData this[int index] {
      get { return Get(index); }
    }

    /// <summary>모든 AZData 중 Name값이 설정되고, Name값이 지정된 Name값과 일치하는 자료의 목록을 반환</summary>
    /// <param name="name">string, </param>
    public AZList this[string name] {
      get { return Get(name); }
    }

    /// <summary>모든 AZData자료에 대해 개별로 T형식으로 Convert()를 수행 후 T형식의 배열로 반환</summary>
    public T[] Convert<T>() {
      T[] rtnValue = new T[Size()];
      for (int cnti = 0; cnti < Size(); cnti++) {
        rtnValue[cnti] = Get(cnti).Convert<T>();
      }
      return rtnValue;
    }

    /// <summary></summary>
    public AZList GetList(int start_index) {
      return GetList(start_index, Size() - start_index);
    }

    /// <summary></summary>
    public AZList GetList(int start_index, int length) {
      if (start_index < 0 || length < 1 || start_index >= Size()) {
        throw new Exception("start_index or length value is invalid");
      }
      int end_index = start_index + length;
      if (end_index >= Size()) end_index = Size();
      //
      AZList rtnValue = new AZList();
      for (int cnti=start_index; cnti<end_index; cnti++) {
        rtnValue.Add(Get(cnti));
      }
      return rtnValue;
    }

    /// <summary></summary>
    public T[] GetList<T>(int start_index) {
      return GetList<T>(start_index, Size() - start_index);
    }

    /// <summary></summary>
    public T[] GetList<T>(int start_index, int length) {
      return GetList(start_index, length).Convert<T>();
    }

    /// AttributeData 에서 사용할 key:value 에 대응하는 자료형 클래스
    private class KeyValue {
      private string key;
      private object value;
      /// <summary>기본 생성자</summary>
      public KeyValue() {
        this.key = "";
        this.value = null;
      }

      /// <summary></summary>
      public KeyValue(string key, object value) {
        this.key = key;
        this.value = value;
      }

      public string GetKey() { return this.key; }
      public object GetValue() { return this.value; }
      public void SetValue(object value) { this.value = value; }
      override public string ToString() { return GetKey() + ":" + GetValue(); }
    }

    /// 속성값(attribute)에 대한 자료 저장용 클래스
    public class AttributeData {
      private List<KeyValue> attribute_list;

      /// <summary></summary>
      public AttributeData() {
        this.attribute_list = new List<KeyValue>();
      }

      /// <summary></summary>
      public object Add(string key, object value) {
        this.attribute_list.Add(new KeyValue(key, value));
        return value;
      }

      /// <summary></summary>
      public object InsertAt(int p_index, string key, object value) {
        object rtn_value = null;
        if (p_index > -1 && p_index < Size()) {
          this.attribute_list.Insert(p_index, new KeyValue(key, value));
          rtn_value = value;
        }
        return value;
      }

      /// <summary></summary>
      public object InsertBefore(string target_key, string key, object value) {
        object rtn_value = null;
        int index = IndexOf(target_key);
        if (index > -1) {
          this.attribute_list.Insert(index, new KeyValue(key, value));
          rtn_value = value;
        }
        return value;
      }

      /// <summary></summary>
      public object InsertAfter(string target_key, string key, object value) {
        object rtn_value = null;
        int index = IndexOf(target_key);
        if (index < Size() - 1) {
          this.attribute_list.Insert(index + 1, new KeyValue(key, value));
          rtn_value = value;
        }
        else if (index == Size() - 1) {
          Add(key, value);
          rtn_value = value;
        }
        return value;
      }

      /// <summary></summary>
      public object Remove(string key) {
        object rtn_value = null;
        for (int cnti = 0; cnti < this.attribute_list.Count; cnti++) {
          if (this.attribute_list[cnti].GetKey().Equals(key)) {
            rtn_value = this.attribute_list[cnti].GetValue();
            this.attribute_list.RemoveAt(cnti);
            break;
          }
        }
        return rtn_value;
      }

      /// <summary></summary>
      public object Remove(int index) {
        object rtn_value = null;
        if (index > -1 || index < Size()) {
          rtn_value = Get(index);
          this.attribute_list.RemoveAt(index);
        }
        return rtn_value;
      }

      /// <summary></summary>
      public object RemoveAt(int index) {
        return Remove(index);
      }

      /// <summary></summary>
      public void Clear() {
        this.attribute_list.Clear();
      }

      /// <summary></summary>
      public int IndexOf(string key) {
        int rtn_value = -1;
        for (int cnti = 0; cnti < this.attribute_list.Count; cnti++) {
          if (this.attribute_list[cnti].GetKey().Equals(key)) {
            rtn_value = cnti;
            break;
          }
        }
        return rtn_value;
      }

      /// <summary></summary>
      public object Get(string key) {
        object rtn_value = null;
        for (int cnti = 0; cnti < this.attribute_list.Count; cnti++) {
          if (this.attribute_list[cnti].GetKey().Equals(key)) {
            rtn_value = this.attribute_list[cnti].GetValue();
            break;
          }
        }
        return rtn_value;
      }

      /// <summary></summary>
      public object Get(int index) {
        object rtn_value = null;
        if (index > -1 || index < Size()) {
          rtn_value = this.attribute_list[index].GetValue();
        }
        return rtn_value;
      }

      /// <summary></summary>
      public object Set(string key, object value) {
        object rtn_value = null;
        for (int cnti = 0; cnti < this.attribute_list.Count; cnti++) {
          if (this.attribute_list[cnti].GetKey().Equals(key)) {
            rtn_value = this.attribute_list[cnti].GetValue();
            this.attribute_list[cnti].SetValue(value);
            break;
          }
        }
        return rtn_value;
      }

      /// <summary></summary>
      public int Size() {
        return this.attribute_list.Count;
      }

      /// <summary></summary>
      public string GetKey(int index) {
        return this.attribute_list[index].GetKey();
      }

      /// <summary></summary>
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