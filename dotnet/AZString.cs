using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Com.Mparang.AZLib {
	public class AZString {
		private object value_object;
		private static object lock_object = new object();

		// property
		public string Value { get { return String(); } }

		public enum ENCODE {
			SQL, XSS, JSON, HTML, XML
		}

		public enum DECODE {
			SQL, JSON, HTML
		}

		public enum RANDOM_TYPE {
			ALPHABET_ONLY, NUMBER_ONLY, ALPHABET_AND_NUMBER
		}

		/*
		public const int RANDOM_ALPHABET_ONLY = -101;
		public const int RANDOM_NUMBER_ONLY = -102;
		public const int RANDOM_ALPHABET_NUMBER = -103;
		*/

		/// <summary></summary>
		public AZString(object pString) { this.value_object = pString; }

		/// <summary></summary>
		public static AZString Init(object pString) { return new AZString(pString); }

		/// <summary></summary>
		public string String() { return String(""); }

		/// <summary></summary>
		public string String(string pDefault) {
			string rtnValue = "";
			if (this.value_object == null) {
				rtnValue = pDefault;
			}
			else {
				switch (Type.GetTypeCode(this.value_object.GetType())) {
					case TypeCode.DateTime:
						try {
						rtnValue = ((DateTime)this.value_object).ToString("yyyy-MM-dd HH:mm:ss.fff");
						}
						catch (Exception ex) {
							string msg = ex.Message;
							rtnValue = pDefault;
						}
						break;
					default:
						try {
							rtnValue = this.value_object.ToString();
						}
						catch (Exception ex) {
							string msg = ex.Message;
							rtnValue = pDefault;
						}
						break;
				}
				/*
				if (this.value_object is int) {
					try {
						rtnValue = "" + this.value_object;
					}
					catch (Exception ex) {
						string msg = ex.Message;
						rtnValue = pDefault;
					}
				}
				else {
					try {
						rtnValue = this.value_object.ToString();
					}
					catch (Exception ex) {
						string msg = ex.Message;
						rtnValue = pDefault;
					}
				}
				*/
			}
			return rtnValue;
		}

		/// <summary></summary>
		override public string ToString() { return String(); }

		/// <summary></summary>
		public int ToInt() { return ToInt(0); }

		/// <summary></summary>
		public int ToInt(int pDefaultValue) {
			int rtnValue = 0;
			if (this.value_object == null) {
				rtnValue = pDefaultValue;
			}
			else {
				try {
					rtnValue = Convert.ToInt32(this.value_object.ToString());
				}
				catch (Exception ex) {
					string msg = ex.Message;
					rtnValue = pDefaultValue;
				}
			}
			return rtnValue;
		}

		/// <summary></summary>
		public static int ToInt(string pValue, int pDefaultValue) {
			int rtnValue = 0;
			try {
				rtnValue = Convert.ToInt32(pValue);
			}
			catch (Exception ex) {
				string msg = ex.Message;
				rtnValue = pDefaultValue;
			}
			return rtnValue;
		}

		/// <summary></summary>
		public static int ToInt(string pValue) { return AZString.ToInt(pValue, 0); }

		/// <summary></summary>
		public long ToLong() { return ToLong(0); }

		/// <summary></summary>
		public long ToLong(long pDefaultValue) {
			long rtnValue = 0;
			if (this.value_object == null) {
				rtnValue = pDefaultValue;
			}
			else {
				try {
					rtnValue = Convert.ToInt64(this.value_object.ToString());
				}
				catch (Exception ex) {
					string msg = ex.Message;
					rtnValue = pDefaultValue;
				}
			}
			return rtnValue;
		}

		/// <summary></summary>
		public static long ToLong(string pValue, long pDefaultValue) {
			long rtnValue = 0;
			try {
				rtnValue = Convert.ToInt64(pValue);
			}
			catch (Exception ex) {
				string msg = ex.Message;
				rtnValue = pDefaultValue;
			}
			return rtnValue;
		}

		/// <summary></summary>
		public static long ToLong(string pValue) { return AZString.ToLong(pValue, 0); }

		/// <summary></summary>
		public float ToFloat() { return ToFloat(0.0f); }

		/// <summary></summary>
		public float ToFloat(float pDefaultValue) {
			float rtnValue = 0.0f;
			if (this.value_object == null) {
				rtnValue = pDefaultValue;
			}
			else {
				try {
					rtnValue = (float)Convert.ToDouble(this.value_object.ToString());
				}
				catch (Exception ex) {
					string msg = ex.Message;
					rtnValue = pDefaultValue;
				}
			}
			return rtnValue;
		}

		/// <summary></summary>
		public static float ToFloat(string pValue, float pDefaultValue) {
			float rtnValue = 0.0f;
			try {
				rtnValue = (float)Convert.ToDouble(pValue);
			}
			catch (Exception ex) {
				string msg = ex.Message;
				rtnValue = pDefaultValue;
			}
			return rtnValue;
		}

		/// <summary></summary>
		public static float ToFloat(string pValue) { return AZString.ToFloat(pValue, 0.0f); }

		/// <summary></summary>
		public DateTime ToDateTime() {
			return AZString.ToDateTime(this.Value);
		}

		/// <summary></summary>
		public DateTime ToDateTime(DateTime? pDefaultValue) {
			return AZString.ToDateTime(this.Value, pDefaultValue);
		}

		/// <summary></summary>
		public static DateTime ToDateTime(string pValue) {
			return ToDateTime(pValue, null);
		}

		/// <summary></summary>
		public static DateTime ToDateTime(string pValue, DateTime? pDefaultValue) {
			DateTime rtnValue = new DateTime();
			if (pDefaultValue.HasValue) rtnValue = pDefaultValue.Value;
			try {
				rtnValue = DateTime.Parse(pValue.Trim());
			}
			catch (Exception) { }
			return rtnValue;
		}

		/// <summary></summary>
		public static string Random() {
			int randomLength = 6;
			Random random = new Random(Guid.NewGuid().GetHashCode());
			random.Next();
			int rndValue = random.Next(12);
			if (rndValue >= 6) rndValue = randomLength;
			return AZString.Random(rndValue, RANDOM_TYPE.ALPHABET_AND_NUMBER, true);
		}

		/// <summary></summary>
		public static string Random(int pLength) {
			return AZString.Random(pLength, RANDOM_TYPE.ALPHABET_AND_NUMBER, true);
		}

		/*
		/// Created in 2015-07-02, leeyonghun
		public static string Random(int pLength, int pRandomType, Boolean pCaseSensitive) {
			RANDOM_TYPE type = RANDOM_TYPE.ALPHABET_ONLY;
			switch (pRandomType) {
				case AZString.RANDOM_NUMBER_ONLY:
					type = RANDOM_TYPE.NUMBER_ONLY;
					break;
				case AZString.RANDOM_ALPHABET_NUMBER:
					type = RANDOM_TYPE.ALPHABET_AND_NUMBER;
					break;
				case AZString.RANDOM_ALPHABET_ONLY:
					type = RANDOM_TYPE.ALPHABET_ONLY;
					break;
			}
			return Random(pLength, type, pCaseSensitive);
		}
		*/

		/// <summary></summary>
		public static string Random(int pLength, string pSourceString) {
			/*StringBuilder rtnValue = new StringBuilder();
			Random random = new Random();
			random.Next();*/

			if (pSourceString.Length < 1) {
				pSourceString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
			}

			/*for (int cnti = 0; cnti < pLength; cnti++) {
				rtnValue.Append(pSourceString.Substring(random.Next(pSourceString.Length), 1));
			}*/
			return AZString.Init(pSourceString).MakeRandom(pLength).String();
		}
		
		/// <summary></summary>
		public static string Random(int pLength, RANDOM_TYPE pRandomType, Boolean pCaseSensitive) {
			string sourceString = "";
			switch (pRandomType) {
				case RANDOM_TYPE.NUMBER_ONLY:
					sourceString = "1234567890";
					break;
				case RANDOM_TYPE.ALPHABET_AND_NUMBER:
					sourceString = "1234567890abcdefghijklmnopqrstuvwxyz";
					break;
				case RANDOM_TYPE.ALPHABET_ONLY:
					sourceString = "1234567890abcdefghijklmnopqrstuvwxyz";
					if (pCaseSensitive) sourceString += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
					break;
			}
			return AZString.Init(sourceString).MakeRandom(pLength).String();
		}

		/// <summary></summary>
		public AZString MakeRandom(int pLength) {
			//this.value_object = AZString.Random(pLength, Value);
			lock (lock_object) {
				string sourceString = Value;

				StringBuilder rtnValue = new StringBuilder();
				Random random = new Random(Guid.NewGuid().GetHashCode());
				random.Next();

				if (sourceString.Length < 1) {
					sourceString = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
				}

				for (int cnti = 0; cnti < pLength; cnti++) {
					rtnValue.Append(sourceString.Substring(random.Next(sourceString.Length), 1));
				}
				this.value_object = rtnValue.ToString();
			}
			return this;
		}

		/// <summary></summary>
		public static string Repeat(string pString, int pLength) {
			StringBuilder rtnValue = new StringBuilder();
			for (int cnti = 0; cnti < pLength; cnti++) {
				rtnValue.Append(pString);
			}
			return rtnValue.ToString();
		}

		/// <summary></summary>
		public AZString Repeat(int pLength) {
			/*
			StringBuilder rtnValue = new StringBuilder();
			for (int cnti = 0; cnti < pLength; cnti++) {
					rtnValue.Append(this.value_object);
			}
			this.value_object = rtnValue.ToString();
			*/
			this.value_object = AZString.Repeat(Value, pLength);
			return this;
		}

		/// <summary></summary>
		public static string Wrap(string pString, string pWrapString, bool pForce) {
			StringBuilder rtnValue = new StringBuilder();
			rtnValue.AppendFormat("{0}{1}{2}", !pForce && pString.StartsWith(pWrapString) ? "" : pWrapString, pString, !pForce && pString.EndsWith(pWrapString) ? "" : pWrapString);
			return rtnValue.ToString();
		}

		/// <summary></summary>
		public static string Wrap(string pString, string pWrapString) {
			return Wrap(pString, pWrapString, false);
		}

		/// <summary></summary>
		public AZString Wrap(string pWrapString, bool pForce) {
			this.value_object = AZString.Wrap(Value, pWrapString, pForce);
			return this;
		}

		/// <summary></summary>
		public AZString Wrap(string pWrapString) {
			this.value_object = AZString.Wrap(Value, pWrapString, false);
			return this;
		}

		/// <summary></summary>
		public static string Format(string pSrc, string pFormat, string pTargetFormat) {
			string rtnValue = pTargetFormat;

			try {
				String divString, compString;
				int divIndex, compIndex;

				for (int cnti = 0; cnti < rtnValue.Length; cnti++) {
					compString = pTargetFormat.Substring(cnti);
					char dmyString = compString[0];

					//compIndex = getStringContCnt(compString, dmyString);
					compIndex = 0;
					for (int cntk = 0; cntk < compString.Length; cntk++) {
						if (!compString[cntk].Equals(dmyString)) break;
						compIndex++;
					}

					divString = compString.Substring(0, compIndex);
					if ((divIndex = pFormat.IndexOf(divString)) > -1) {
						rtnValue = rtnValue.Replace(divString, pSrc.Substring(divIndex, divString.Length));
					}
					else {
						continue;
					}
					cnti += compIndex - 1;
				}
			}
			catch (Exception ex) {
				throw new Exception("Format", ex);
			}
			return rtnValue;
		}

		/// <summary></summary>
		public AZString Format(string pFormat, string pTarget) {
			this.value_object = AZString.Format(String(), pFormat, pTarget);
			return this;
		}

		/// <summary></summary>
		public string[] ToStringArray(int pLength) {
			string[] rtnValue = new String[(int)Math.Ceiling(this.String().Length / (double)pLength)];

			string src = this.String();
			int idx = 0;
			try {
				while (true) {
					if (src.Length > pLength) {
						rtnValue[idx] = src.Substring(0, pLength);
						src = src.Substring(pLength);
						idx++;
					}
					else {
						rtnValue[idx] = src;
						break;
					}
				}
			}
			catch (Exception ex) {
				throw new Exception("", ex);
			}
			finally {
				src = null;
			}
			return rtnValue;
		}

		/// <summary></summary>
		public static String[] ToStringArray(string pSrc, int pLength) {
			return new AZString(pSrc).ToStringArray(pLength);
		}

		/// <summary></summary>
		public AZString Encode(ENCODE pEncode) {
			switch (pEncode) {
				case ENCODE.SQL: this.value_object = ToSQLSafeEncoding(); break;
				case ENCODE.XSS: this.value_object = ToXSSSafeEncoding(); break;
				case ENCODE.JSON: this.value_object = ToJSONSafeEncoding(); break;
				case ENCODE.HTML: this.value_object = ToHTMLSafeEncoding(); break;
				case ENCODE.XML: this.value_object = ToXMLSafeEncoding(); break;
			}
			return this;
		}

		/// <summary></summary>
		public static string Encode(ENCODE pEncode, string pSrc) {
			return AZString.Init(pSrc).Encode(pEncode).Value;
		}

		/// <summary></summary>
		public AZString Decode(DECODE pDecode) {
			switch (pDecode) {
				case DECODE.SQL: this.value_object = ToSQLSafeDecoding(); break;
				case DECODE.JSON: this.value_object = ToJSONSafeDecoding(); break;
				case DECODE.HTML: this.value_object = ToHTMLSafeDecoding(); break;
			}
			return this;
		}

		/// <summary></summary>
		public static string Decode(DECODE pDecode, string pSrc) {
			return AZString.Init(pSrc).Decode(pDecode).Value;
		}

		#region private encode/decode

		/// <summary></summary>
		private string ToXSSSafeEncoding() {
			string rtnValue;
			rtnValue = this.String();
			rtnValue = rtnValue.Replace("<s", "&lt;&#115;");
			rtnValue = rtnValue.Replace("<S", "&lt;&#83;");
			rtnValue = rtnValue.Replace("<i", "&lt;&#105;");
			rtnValue = rtnValue.Replace("<I", "&lt;&#73;");
			return rtnValue;
		}

		/// <summary></summary>
		private string ToXMLSafeEncoding() {
			string rtnValue;
			rtnValue = this.String();
			rtnValue = rtnValue.Replace("'", "\\'");
			rtnValue = rtnValue.Replace("\r\n", " ");
			rtnValue = rtnValue.Replace("&", "&amp;");
			rtnValue = rtnValue.Replace("'", "&apos;");
			rtnValue = rtnValue.Replace("\"", "&quot;");
			rtnValue = rtnValue.Replace("<", "&lt;");
			rtnValue = rtnValue.Replace(">", "&gt;");
			return rtnValue;
		}

		/// <summary></summary>
		private string ToHTMLSafeEncoding() {
			string rtnValue;
			rtnValue = this.String();
			rtnValue = rtnValue.Replace("&", "&amp;");
			rtnValue = rtnValue.Replace("<", "&lt;");
			rtnValue = rtnValue.Replace(">", "&gt;");
			rtnValue = rtnValue.Replace("\"", "&quot;");
			return rtnValue;
		}

		/// <summary></summary>
		private string ToHTMLSafeDecoding() {
			string rtnValue;
			rtnValue = this.String();
			rtnValue = rtnValue.Replace("&lt;", "<");
			rtnValue = rtnValue.Replace("&gt;", ">");
			rtnValue = rtnValue.Replace("&quot;", "\"");
			rtnValue = rtnValue.Replace("&amp;", "&");
			return rtnValue;
		}

		/// <summary></summary>
		private string ToSQLSafeEncoding() {
			string rtnValue;
			rtnValue = this.String();
			//rtnValue = rtnValue.Replace("&nbsp;", "&nbsp");
			rtnValue = rtnValue.Replace("#", "&#35;");			// # 위치 확인!
			rtnValue = rtnValue.Replace(";", "&#59;");
			rtnValue = rtnValue.Replace("'", "&#39;");
			rtnValue = rtnValue.Replace("--", "&#45;&#45;");
			rtnValue = rtnValue.Replace("\\", "&#92;");
			rtnValue = rtnValue.Replace("*", "&#42;");
			//rtnValue = rtnValue.Replace("&nbsp", "&nbsp;");
			return rtnValue;
		}

		/// <summary></summary>
		private string ToSQLSafeDecoding() {
			string rtnValue;
			rtnValue = this.String();
			//rtnValue = rtnValue.Replace("&nbsp;", "&nbsp");
			rtnValue = rtnValue.Replace("&#42;", "*");
			rtnValue = rtnValue.Replace("&#92;", "\\");
			rtnValue = rtnValue.Replace("&#45;&#45;", "--");
			rtnValue = rtnValue.Replace("&#39;", "'");
			rtnValue = rtnValue.Replace("&#35;", "#");
			rtnValue = rtnValue.Replace("&#59;", ";");
			rtnValue = rtnValue.Replace("&#35;", "#");
			//rtnValue = rtnValue.Replace("&nbsp", "&nbsp;");
			return rtnValue;
		}

		/// <summary></summary>
		private string ToJSONSafeEncoding() {
			string rtnValue;
			rtnValue = this.String();
			rtnValue = rtnValue.Replace("\\", "\\\\");
			rtnValue = rtnValue.Replace("\"", "\\\"");
			rtnValue = rtnValue.Replace("\b", "\\b");
			rtnValue = rtnValue.Replace("\f", "\\f");
			rtnValue = rtnValue.Replace("\n", "\\n");
			rtnValue = rtnValue.Replace("\r", "\\r");
			rtnValue = rtnValue.Replace("\t", "\\t");
			return rtnValue;
		}

		/// <summary></summary>
		private string ToJSONSafeDecoding() {
			string rtnValue;
			rtnValue = this.String();
			rtnValue = rtnValue.Replace("\\t", "\t");
			rtnValue = rtnValue.Replace("\\r", "\r");
			rtnValue = rtnValue.Replace("\\n", "\n");
			rtnValue = rtnValue.Replace("\\f", "\f");
			rtnValue = rtnValue.Replace("\\b", "\b");
			rtnValue = rtnValue.Replace("\\\"", "\"");
			rtnValue = rtnValue.Replace("\\\\", "\\");
			return rtnValue;
		}
		#endregion

		/// <summary></summary>
		public T To<T>() {
			return AZString.To<T>(this.Value);
		}

		/// <summary></summary>
		public T To<T>(T pDefaultValue) {
			return AZString.To<T>(this.Value, pDefaultValue);
		}

		/// <summary></summary>
		public static T To<T>(object pValue) {
			object obj;
			if (typeof(T) == typeof(string)) {
				obj = "";
			}
			else {
				obj = Activator.CreateInstance(typeof(T));
			}
			return To<T>(pValue, obj);
		}

		/// <summary></summary>
		public static T To<T>(object pValue, object pDefaultValue) {
			object obj;
			if (typeof(T) == typeof(string)) {
				obj = "";
			}
			else {
				obj = Activator.CreateInstance(typeof(T));
			}

			if (pValue == null) {
				obj = pDefaultValue;
			}
			else {
				if (typeof(T) == typeof(int)) {
					obj = AZString.Init(pValue).ToInt((int)pDefaultValue);
				}
				else if (typeof(T) == typeof(float)) {
					obj = AZString.Init(pValue).ToFloat((int)pDefaultValue);
				}
				else if (typeof(T) == typeof(string)) {
					obj = AZString.Init(pValue).String((string)pDefaultValue);
				}
				else if (typeof(T) == typeof(DateTime)) {
					obj = AZString.Init(pValue).ToDateTime((DateTime)pDefaultValue);
				}
				else if (typeof(T) == typeof(AZData)) {
					obj = AZData.From<T>((T)pValue);
				}
			}
			return (T)obj;
		}

		/// <summary></summary>
		public class XML {
			private string xml;

			/// <summary></summary>
			public XML(string p_xml) { this.xml = p_xml; }

			/// <summary></summary>
			public static AZString.XML Init(string p_xml) { return new AZString.XML(p_xml); }

			/// <summary></summary>
			private static string ReplaceInnerDQ(string pString, char p_replace_chr) { return ReplaceInner(pString, "\"", "\"", p_replace_chr); }
			private static string ReplaceInner(string pString, string pStartString, string pEndString, char pReplaceChr) {
				StringBuilder builder = new StringBuilder();

				int dqStartIdx = -1, dqEndIdx = -1, idx = 0, prevIdx = 0;
				int startCharExistAfterDQStart = 0, idxAfterDQStart = -1;	 // appended
				bool inDQ = false;
				while (true) {
					idx = inDQ ? pString.IndexOf(pEndString, idx) : pString.IndexOf(pStartString, idx);
					if (idx > -1) {
						if (idx == 0 || idx > 0 && pString[idx - 1] != '\\') {
							if (inDQ) {
									// appended
									idxAfterDQStart = pString.IndexOf(pStartString, startCharExistAfterDQStart == 0 ? (dqStartIdx + 1) : idxAfterDQStart + 1);
									if (idxAfterDQStart > -1 && idxAfterDQStart < idx && pString[idxAfterDQStart - 1] != '\\') {
										startCharExistAfterDQStart = 1;
										idx += 1;
										continue;
									}
									// appended
									dqEndIdx = idx;
									inDQ = false;

									if (dqEndIdx > -1) {
										builder.Append(new String(pReplaceChr, dqEndIdx - dqStartIdx - 1));
									}
								}
								else {
									dqStartIdx = idx;
									inDQ = true;
									builder.Append(pString.Substring(prevIdx, idx - prevIdx) + pStartString);
								}
							}
							prevIdx = idx;
							idx++;
						}
					else {
						builder.Append(pString.Substring(dqEndIdx < 0 ? 0 : dqEndIdx));
						break;
					}
				}
				return builder.ToString();
			}

			/// <summary></summary>
			public AZData ToAZData() { return AZString.XML.ToAZData(this.xml); }
			public static AZData ToAZData(string p_xml_string) {
				AZData rtnValue = new AZData();
				p_xml_string = p_xml_string.Trim();
				if (p_xml_string.Length < 3) {
					return rtnValue;
				}
				if (p_xml_string.ToLower().StartsWith("<?xml")) {
					int index_xml = p_xml_string.IndexOf("?>");
					if (index_xml < 0 || index_xml >= p_xml_string.Length) {
						return rtnValue;
					}
					else {
						p_xml_string = p_xml_string.Substring(index_xml + 2);
					}
				}
				if (p_xml_string.StartsWith("<") && p_xml_string.EndsWith(">")) {
					p_xml_string = p_xml_string.Substring(1, p_xml_string.Length - 1).Trim();
					string rmDQStr = ReplaceInnerDQ(p_xml_string, '_');

					bool tag_inner_string_exist = true;
					int idx_closer = rmDQStr.IndexOf(">");
					string tag_name = "";
					if (rmDQStr.IndexOf(" ") > idx_closer || rmDQStr.IndexOf(" ") < 0) {
						tag_name = p_xml_string.Substring(0, idx_closer).Trim();
						tag_inner_string_exist = false;
					}
					else {
						tag_name = p_xml_string.Substring(0, rmDQStr.IndexOf(" ")).Trim();
					}
					rtnValue.Name = tag_name;

					int idx_ender = rmDQStr.IndexOf("</" + tag_name + ">");
					string tag_inner_string = "";
					if (tag_inner_string_exist) {
						tag_inner_string = p_xml_string.Substring(p_xml_string.IndexOf(" "), idx_closer - p_xml_string.IndexOf(" ")).Trim();
					}
					string tag_inner_string_rpDQ = ReplaceInnerDQ(tag_inner_string, '_');
					int pre_idx = -1, start_idx = 0;
					while (tag_inner_string_exist) {
						if ((start_idx = tag_inner_string_rpDQ.IndexOf(" ", ++pre_idx)) > -1) {
							string attribute = tag_inner_string.Substring(pre_idx, start_idx - pre_idx);
							string attribute_name = "", attribute_value = "";
							if (attribute.IndexOf("=") < 1) {
								attribute_name = attribute.Trim();
								attribute_value = null;
							}
							else {
								attribute_name = attribute.Substring(0, attribute.IndexOf("=")).Trim();
								attribute_value = attribute.Substring(attribute.IndexOf("=") + 1).Trim();
								if (attribute_value.Length > 1) {
									if (attribute_value.StartsWith("\"") && attribute_value.EndsWith("\"")) {
										attribute_value = attribute_value.Substring(1, attribute_value.Length - 2);
									}
								}
							}
							rtnValue.Attribute.Add(attribute_name, attribute_value);
							pre_idx = start_idx;
						}
						else if ((start_idx = tag_inner_string_rpDQ.IndexOf(" ", ++pre_idx)) < 0 && tag_inner_string_rpDQ.Substring(pre_idx + 1).Length > 2) {
							string attribute = tag_inner_string.Substring(pre_idx - 1); ;
							string attribute_name = "", attribute_value = "";
							if (attribute.IndexOf("=") < 1) {
								attribute_name = attribute.Trim();
								attribute_value = null;
							}
							else {
								attribute_name = attribute.Substring(0, attribute.IndexOf("=")).Trim();
								attribute_value = attribute.Substring(attribute.IndexOf("=") + 1).Trim();
								if (attribute_value.Length > 1) {
									if (attribute_value.StartsWith("\"") && attribute_value.EndsWith("\"")) {
										attribute_value = attribute_value.Substring(1, attribute_value.Length - 2);
									}
								}
							}
							rtnValue.Attribute.Add(attribute_name, attribute_value);
							break;
						}
						else {
							break;
						}
					}
					string tag_lower_string = p_xml_string.Substring(idx_closer + 1, idx_ender - (idx_closer + 1)).Trim();
					while (tag_lower_string.Trim().Length > 0) {
						tag_lower_string = tag_lower_string.Trim();
						if (tag_lower_string.StartsWith("<") && !tag_lower_string.StartsWith("<!")) {
							string inner_rmDQStr = ReplaceInnerDQ(tag_lower_string, '_');
							int inner_idx_closer = inner_rmDQStr.IndexOf(">");
							string inner_tag_name = "";
							if (rmDQStr.IndexOf(" ") > idx_closer || rmDQStr.IndexOf(" ") < 0) {
								inner_tag_name = tag_lower_string.Substring(1, inner_idx_closer - 1).Trim();
							}
							else {
								inner_tag_name = tag_lower_string.Substring(1, inner_rmDQStr.IndexOf(" ") - 1).Trim();
							}

							int inner_idx_ender = inner_rmDQStr.IndexOf("</" + inner_tag_name + ">");
							string inner_data_string = "";
							if (inner_idx_ender < 0) {
								inner_data_string = tag_lower_string;
								tag_lower_string = "";
							}
							else {
								inner_data_string = tag_lower_string.Substring(0, inner_idx_ender + ("</" + inner_tag_name + ">").Length);
								tag_lower_string = tag_lower_string.Substring(inner_data_string.Length);
							}

							if (!rtnValue.HasKey(inner_tag_name)) {
								AZList child_list = new AZList();
								child_list.Add(ToAZData(inner_data_string));
								rtnValue.Add(inner_tag_name, child_list);
								child_list = null;
							}
							else {
								AZList child_list = rtnValue.GetList(inner_tag_name);
								child_list.Add(ToAZData(inner_data_string));
								rtnValue.Set(inner_tag_name, child_list);
								child_list = null;
							}
						}
						else {
							string inner_rmDQStr = ReplaceInnerDQ(tag_lower_string, '_');
							string inner_rmCDataStr = ReplaceInner(inner_rmDQStr, "<![CDATA[", "]]>", '_');

							int inner_end_index = inner_rmCDataStr.IndexOf("<");
							if (inner_end_index < 0) {
								rtnValue.Value = tag_lower_string;
								tag_lower_string = "";
							}
							else {
								rtnValue.Value = tag_lower_string.Substring(0, inner_end_index);
								tag_lower_string = tag_lower_string.Substring(inner_end_index);
							}
						}
					}
				}

				return rtnValue;
			}
		}

		/// <summary></summary>
		public class JSON {
			private string json;

			/// <summary></summary>
			public JSON(string pJson) { this.json = pJson; }

			/// <summary></summary>
			public static AZString.JSON Init(string pJson) { return new AZString.JSON(pJson); }

			/// <summary>객체의 접근자가 public 인 property들에 대해 json 형식으로 변경 반환 처리</summary>
			public static string Convert<T>(T pTarget) {
				return AZData.From<T>(pTarget).ToJsonString();
			}

			/// <summary></summary>
			private static string RemoveInnerDQ(string pString) { return RemoveInner(pString, '"', '"'); }

			/// <summary></summary>
			private static string RemoveInner(string pString, char pStartChr, char pEndChr) {
				StringBuilder builder = new StringBuilder();

				int dqStartIdx = -1, dqEndIdx = -1, idx = 0, prevIdx = 0;
				int startCharExistAfterDQStart = 0, idxAfterDQStart = -1;	 // appended
				bool inDQ = false;
				while (true) {
					idx = inDQ ? pString.IndexOf(pEndChr, idx) : pString.IndexOf(pStartChr, idx);
					if (idx > -1) {
						if (idx == 0 || idx > 0 && pString[idx - 1] != '\\') {
							if (inDQ) {
								idxAfterDQStart = pString.IndexOf(pStartChr, startCharExistAfterDQStart == 0 ? (dqStartIdx + 1) : idxAfterDQStart + 1);
								if (idxAfterDQStart > -1 && idxAfterDQStart < idx && pString[idxAfterDQStart - 1] != '\\') {
									startCharExistAfterDQStart = 1;
									idx += 1;
									continue;
								}

								dqEndIdx = idx;
								inDQ = false;
								if (dqEndIdx > -1) {
									builder.Append(new String(' ', dqEndIdx - dqStartIdx - 1));
								}
							}
							else {
								dqStartIdx = idx;
								inDQ = true;
								builder.Append(pString.Substring(prevIdx, idx - prevIdx) + pStartChr);
							}
						}
						prevIdx = idx;
						idx++;
					}
					else {
						builder.Append(pString.Substring(dqEndIdx < 0 ? 0 : dqEndIdx));
						break;
					}
				}
				return builder.ToString();
			}

			/// <summary></summary>
			public AZData ToAZData() { return AZString.JSON.ToAZData(this.json); }
			public static AZData ToAZData(string pJsonString) {
				AZData rtnValue = new AZData();
				pJsonString = pJsonString.Trim();
				if (pJsonString.Length < 3) {
						return rtnValue;
				}
				if (pJsonString[0] == '{' && pJsonString[pJsonString.Length - 1] == '}') {
					pJsonString = pJsonString.Substring(1, pJsonString.Length - 2).Trim();
					string rmDQStr = RemoveInnerDQ(pJsonString);
					string rmMStr = RemoveInner(rmDQStr, '[', ']');
					string rmStr = RemoveInner(rmMStr, '{', '}');

					int idx = 0, preIdx = 0;
					while (true) {
						idx = rmStr.IndexOf(",", idx);
						if (idx == -1 && preIdx == 0) {
							string dataStr = "";
							dataStr = pJsonString;
							dataStr = dataStr.Trim();

							int key_value_idx = dataStr.IndexOf(":");
							string key = dataStr.Substring(0, key_value_idx).Trim();
							string valueString = dataStr.Substring(key_value_idx + 1).Trim();

							if (key[0] == '"' || key[0] == '\'') key = key.Substring(1, key.Length - 2);

							object value = null;
							if (valueString[0] == '{') {
								value = ToAZData(valueString);
							}
							else if (valueString[0] == '[') {
								valueString = valueString.Substring(1, valueString.Length - 2);
								while (true) {
									if (valueString.StartsWith(" ") || valueString.StartsWith("\r") || valueString.StartsWith("\n") || valueString.StartsWith("\t")) {
										valueString = valueString.Substring(1);
									}
									else { break; }
								}
								while (true) {
									if (valueString.EndsWith(" ") || valueString.EndsWith("\r") || valueString.EndsWith("\n") || valueString.EndsWith("\t")) {
										valueString = valueString.Substring(0, valueString.Length - 1);
									}
									else { break; }
								}
								if (valueString.StartsWith("{") || valueString.Trim().Length == 0) {
									value = ToAZList("[" + valueString + "]");
								}
								else {
									value = valueString.Split(',').Each(x => (x.Trim().StartsWith("'") && x.Trim().EndsWith("'") ? x.Trim().Substring(1, x.Trim().Length - 2) : x.Trim()));
								}
							}
							else if (valueString[0] == '"' || valueString[0] == '\'') {
								value = (String)valueString.Substring(1, valueString.Length - 2);
							}
							else {
								value = valueString;
								if (((String)value).IsNumeric()) {
									if (((String)value).IndexOf(".") > 0) {
										value = ((String)value).ToFloat();
									}
									else {
										value = ((String)value).ToLong();
									}
								}
								else if (((String)value).Equals("true")) {
									value = true;
								}
								else if (((String)value).Equals("false")) {
									value = false;
								}
							}
							rtnValue.Add(key, value);
							break;
						}
						else if (idx > -1 || (idx == -1 && preIdx > 0)) {
							string dataStr = "";
							if (idx > -1) {
								dataStr = pJsonString.Substring(preIdx, idx - preIdx);
							}
							else if (idx == -1) {
								dataStr = pJsonString.Substring(preIdx + 1);
							}
							dataStr = dataStr.Trim();
							if (dataStr[0] == ',') dataStr = dataStr.Substring(1).Trim();
							if (dataStr[dataStr.Length - 1] == ',') dataStr = dataStr.Substring(0, dataStr.Length - 1).Trim();

							int key_value_idx = dataStr.IndexOf(":");
							string key = dataStr.Substring(0, key_value_idx).Trim();
							string valueString = dataStr.Substring(key_value_idx + 1).Trim();

							if (key[0] == '"' || key[0] == '\'') key = key.Substring(1, key.Length - 2);

							object value = null;
							if (valueString[0] == '{') {
								value = ToAZData(valueString);
							}
							else if (valueString[0] == '[') {
								valueString = valueString.Substring(1, valueString.Length - 2);
								while (true) {
									if (valueString.StartsWith(" ") || valueString.StartsWith("\r") || valueString.StartsWith("\n") || valueString.StartsWith("\t")) {
										valueString = valueString.Substring(1);
									}
									else { break; }
								}
								while (true) {
									if (valueString.EndsWith(" ") || valueString.EndsWith("\r") || valueString.EndsWith("\n") || valueString.EndsWith("\t")) {
										valueString = valueString.Substring(0, valueString.Length - 1);
									}
									else { break; }
								}
								if (valueString.StartsWith("{") || valueString.Trim().Length == 0) {
									value = ToAZList("[" + valueString + "]");
								}
								else {
									value = valueString.Split(',').Each(x => (x.Trim().StartsWith("'") && x.Trim().EndsWith("'") ? x.Trim().Substring(1, x.Trim().Length - 2) : x.Trim()));
									string[] values = valueString.Split(',');
									TypeCode typeCode = TypeCode.String;
									for (int cnti=0; cnti<values.Length; cnti++) {
										string col = values[cnti];
										if (col.Equals("true") || col.Equals("false")) {
											typeCode = TypeCode.Boolean;
										}
										else if (!col.StartsWith("\"")) {
											typeCode = TypeCode.Int64;
											if (col.IndexOf(".") > -1) {
												typeCode = TypeCode.Double;
												break;
											}
										}
									}
									switch (typeCode) {
										case TypeCode.String:
											string[] rVal = new string[values.Length];
											for (int cnti=0; cnti<values.Length; cnti++) {
												string col = values[cnti];
												if (col.StartsWith("\"")) col = col.Substring(1);
												if (col.EndsWith("\"")) col = col.Substring(0, col.Length - 1);
												rVal[cnti] = col;
											}
											value = rVal;
											break;
										case TypeCode.Int64:
											long[] rnVal = new long[values.Length];
											for (int cnti=0; cnti<values.Length; cnti++) {
												rnVal[cnti] = values[cnti].ToLong();
											}
											value = rnVal;
											break;
										case TypeCode.Double:
											float[] rfVal = new float[values.Length];
											for (int cnti=0; cnti<values.Length; cnti++) {
												rfVal[cnti] = values[cnti].ToFloat();
											}
											value = rfVal;
											break;
										case TypeCode.Boolean:
											bool[] bfVal = new bool[values.Length];
											for (int cnti=0; cnti<values.Length; cnti++) {
												bfVal[cnti] = values[cnti].Equals("true");
											}
											value = bfVal;
											break;
									}
								}
							}
							else if (valueString[0] == '"' || valueString[0] == '\'') {
								value = (String)valueString.Substring(1, valueString.Length - 2);
							}
							else {
								value = valueString;
								if (((String)value).IsNumeric()) {
									if (((String)value).IndexOf(".") > 0) {
										value = ((String)value).ToFloat();
									}
									else {
										value = ((String)value).ToLong();
									}
								}
								else if (((String)value).Equals("true")) {
									value = true;
								}
								else if (((String)value).Equals("false")) {
									value = false;
								}
							}
							rtnValue.Add(key, value);
							if (idx == -1) break;
							preIdx = idx;
							idx++;
						}
						else {
							break;
						}
					}
				}
				return rtnValue;
			}

			/// <summary></summary>
			public AZList ToAZList() { return AZString.JSON.ToAZList(this.json); }
			public static AZList ToAZList(string pJsonString) {
				AZList rtnValue = new AZList();
				pJsonString = pJsonString.Trim();
				if (pJsonString[0] == '[' && pJsonString[pJsonString.Length - 1] == ']') {
					pJsonString = pJsonString.Substring(1, pJsonString.Length - 2).Trim();
					string rmDQStr = RemoveInnerDQ(pJsonString);
					string rmMStr = RemoveInner(rmDQStr, '[', ']');
					string rmStr = RemoveInner(rmMStr, '{', '}');
					int idx = 0, preIdx = 0;
					while (true) {
						idx = rmStr.IndexOf(",", idx);
						if (idx > -1 || (idx == -1 && preIdx > 0)) {
							string dataStr = "";
							if (idx > -1) {
								dataStr = pJsonString.Substring(preIdx, idx - preIdx);
							}
							else if (idx == -1) {
								dataStr = pJsonString.Substring(preIdx + 1);
							}
							dataStr = dataStr.Trim();
							if (dataStr[0] == ',') dataStr = dataStr.Substring(1).Trim();
							if (dataStr[dataStr.Length - 1] == ',') dataStr = dataStr.Substring(0, dataStr.Length - 1).Trim();
							if (dataStr[0] == '{') rtnValue.Add(ToAZData(dataStr));
							if (idx == -1) break;
							preIdx = idx;
							idx++;
						}
						else {
							if (preIdx < 1 && idx < 0) {
								string dataStr = rmStr.Trim();
								if (dataStr.Length > 1) {
									if (dataStr.StartsWith("{")) {
										dataStr = pJsonString.Substring(0);
										rtnValue.Add(ToAZData(dataStr));
									}
								}
							}
							break;
						}
					}
				}
				return rtnValue;
			}
		}
	}


	/// 확장메소드
	public static class AZStringEx {
		public static AZList ToAZList(this IEnumerable<AZData> src) {
			AZList rtnValue = new AZList();
			foreach (AZData row in src) {
				rtnValue.Add(row);
			}
			return rtnValue;
		}
		
		/// <summary></summary>
		public static string AZFormat(this string pSrc, string pFormat, string pTargetFormat) {
			return AZString.Format(pSrc, pFormat, pTargetFormat);
		}

		/// <summary></summary>
		public static string Format(this DateTime pDate, string pFormat) {
			string rtnValue = "";
			//DateTime date = DateTime.Parse(pSrc);
			rtnValue = "" + pDate.Year.ToString().PadLeft(4, '0') + "-";
			rtnValue += pDate.Month.ToString().PadLeft(2, '0') + "-";
			rtnValue += pDate.Day.ToString().PadLeft(2, '0');
			rtnValue += " " + pDate.Hour.ToString().PadLeft(2, '0') + ":";
			rtnValue += pDate.Minute.ToString().PadLeft(2, '0') + ":";
			rtnValue += pDate.Second.ToString().PadLeft(2, '0');
			rtnValue = AZString.Format(rtnValue, "YYYY-MM-DD hh:mm:ss", pFormat);
			return rtnValue;
		}

		/// <summary></summary>
		public static string Format(this DateTime pDate) {
			return pDate.Format("YYYY-MM-DD hh:mm:ss");
		}

		/// <summary></summary>
		public static int ToInt(this string pSrc, int pDefault) {
			return AZString.ToInt(pSrc, pDefault);
		}

		/// <summary></summary>
		public static int ToInt(this string pSrc) {
			return AZString.ToInt(pSrc);
		}

		/// <summary></summary>
		public static long ToLong(this string pSrc, long pDefault) {
			return AZString.ToLong(pSrc, pDefault);
		}

		/// <summary></summary>
		public static long ToLong(this string pSrc) {
			return AZString.ToLong(pSrc);
		}

		/// <summary></summary>
		public static float ToFloat(this string pSrc, float pDefault) {
			return AZString.ToFloat(pSrc, pDefault);
		}

		/// <summary></summary>
		public static float ToFloat(this string pSrc) {
			return AZString.ToFloat(pSrc);
		}

		/// <summary></summary>
		public static string Random(this string pSrc, int pLength) {
			return AZString.Random(pLength, pSrc);
		}

		/// <summary></summary>
		public static string Repeat(this string pSrc, int pLength) {
			return AZString.Repeat(pSrc, pLength);
		}

		/// <summary></summary>
		public static string Wrap(this string pSrc, string pWrapString, bool pForce) {
			return AZString.Wrap(pSrc, pWrapString, pForce);
		}

		/// <summary></summary>
		public static string Wrap(this string pSrc, string pWrapString) {
			return AZString.Wrap(pSrc, pWrapString, false);
		}

		/// <summary></summary>
		public static string Join<T>(this T[] pSrc, string pSeperator) {
			if (pSrc.GetType() != typeof(int[]) && pSrc.GetType() != typeof(long[]) && pSrc.GetType() != typeof(float[]) &&
				pSrc.GetType() != typeof(double[]) && pSrc.GetType() != typeof(string[]) && pSrc.GetType() != typeof(bool[])) {
					throw new Exception("Only int, float, double, string type supported.");
			}
			StringBuilder rtnValue = new StringBuilder();
			for (int cnti=0; cnti<pSrc.Length; cnti++) {
				if (pSrc[cnti].GetType() == typeof(bool)) {
					bool? bVal = (pSrc[cnti] as bool?);
					rtnValue.Append(bVal.HasValue ? (bVal.Value ? "true" : "false") : "null");
				}
				else {
					rtnValue.Append(pSrc[cnti]);
				}
				if (cnti < pSrc.Length - 1 && pSeperator.Length > 0) {
					rtnValue.Append(pSeperator);
				}
			}
			return rtnValue.ToString();
		}

		/// <summary></summary>
		public static string Join<T>(this T[] pSrc) {
			return Join(pSrc, "");
		}

		/// <summary></summary>
		public static bool Has<T>(this T[] pSrc, T pValue) {
			return IndexOf<T>(pSrc, pValue) > -1 ? true : false;
		}

		/// <summary></summary>
		public static bool Has<T>(this T[] pSrc, T[] pValue) {
			return IndexOf(pValue, pSrc).Count(row => row > -1) == pValue.Length;
		}

		/// <summary></summary>
		public static bool HasAny<T>(this T[] pSrc, T[] pValue) {
			return IndexOf(pSrc, pValue).Count(row => row > -1) > 0;
		}

		/// <summary></summary>
		public static int IndexOf<T>(this T[] pSrc, T pValue) {
			int rtnValue = -1;
			for (int cnti=0; cnti<pSrc.Length; cnti++) {
				if (pSrc[cnti].Equals(pValue)) {
					rtnValue = cnti;
					break;
				}
			}
			return rtnValue;
		}

		/// <summary></summary>
		public static int[] IndexOf<T>(this T[] pSrc, T[] pValue) {
			int[] rtnValue = pValue
				.Select(row => IndexOf(pSrc, row))
				.ToArray();
			return rtnValue;
		}

		/// <summary></summary>
		/// <example>arrs.Each(x => x.Wrap("'"));</example>
		public static T[] Each<T>(this T[] pSrc, Func<T, T> pFunc) {
			T[] rtnValue = pSrc;
			for (int cnti=0; cnti<rtnValue.Length; cnti++) {
				rtnValue[cnti] = pFunc(rtnValue[cnti]);
			}
			return rtnValue;
		}

		/// <summary></summary>
		public static T Push<T>(this T[] pSrc, T pValue) {
			T rtnValue = default(T);
			if (pSrc != null) {
				T[] dValue = new T[pSrc.Length + 1];
				pSrc.CopyTo(dValue, 0);
				dValue[pSrc.Length] = pValue;
				pSrc = dValue;
			}
			return rtnValue;
		}

		/// <summary></summary>
		public static T Pop<T>(this T[] pSrc) {
			T rtnValue = default(T);
			if (pSrc != null) {
				rtnValue = pSrc[pSrc.Length];
				//
				T[] replaceValue = new T[pSrc.Length - 1];
				pSrc.CopyTo(replaceValue, 0);
				pSrc = replaceValue;
			}
			return rtnValue;
		}

		/// <summary></summary>
		public static List<T> ToList<T>(this T[] pSrc) {
			List<T> rtnValue = new List<T>();
			for (int cnti = 0; cnti<pSrc.Length; cnti++) {
				rtnValue.Add(pSrc[cnti]);
			}
			return rtnValue;
		}

		/// <summary></summary>
		public static string Encode(this string pSrc, AZString.ENCODE pEncode) {
			return AZString.Encode(pEncode, pSrc);
		}

		/// <summary></summary>
		public static string Decode(this string pSrc, AZString.DECODE pDecode) {
			return AZString.Decode(pDecode, pSrc);
		}

		/// <summary></summary>
		public static string Encrypt(this string pSrc, AZEncrypt.ENCRYPT pEncrypt, string pKey) {
			return pSrc.Encrypt(pEncrypt, pKey, null);
		}

		/// <summary></summary>
		public static string Encrypt(this string pSrc, AZEncrypt.ENCRYPT pEncrypt, string pKey, string pDefault) {
			string rtnValue = pDefault;
			switch (pEncrypt) {
				case AZEncrypt.ENCRYPT.AES256:
					rtnValue = new AZEncrypt.AES256().Enc(pSrc, pKey);
					break;
			} 
			return rtnValue;
		}

		/// <summary></summary>
		public static string Decrypt(this string pSrc, AZEncrypt.ENCRYPT pEncrypt, string pKey) {
			return pSrc.Decrypt(pEncrypt, pKey, null);
		}

		/// <summary></summary>
		public static string Decrypt(this string pSrc, AZEncrypt.ENCRYPT pEncrypt, string pKey, string pDefault) {
			string rtnValue = pDefault;
			switch (pEncrypt) {
				case AZEncrypt.ENCRYPT.AES256:
					rtnValue = new AZEncrypt.AES256().Dec(pSrc, pKey);
					break;
			} 
			return rtnValue;
		}

		/// <summary></summary>
		public static T To<T>(this object pSrc, float pDefault) {
			return AZString.To<T>(pSrc, pDefault);
		}

		/// <summary></summary>
		public static AZData ToAZData(this string pSrc) {
			return AZString.JSON.ToAZData(pSrc);
		}

		/// <summary></summary>
		public static AZList ToAZList(this string pSrc) {
			return AZString.JSON.ToAZList(pSrc);
		}

		/// <summary></summary>
		public static T To<T>(this object pSrc) {
			return AZString.To<T>(pSrc);
		}

		/// <summary></summary>
		public static string ToJson<T>(this T pSrc) {
			return AZString.JSON.Convert(pSrc);
		}

		/// <summary></summary>
		public static bool IsNumeric(this string s) {
			int idx = 0;
			foreach (char c in s) {
				if (!char.IsDigit(c) && c != '.') {
					if (idx == 0 && c == '-') return true;
					return false;
				}
				idx++;
			}
			return true;
		}

		/// <summary></summary>
		public static string[] GetPropertyNames<T>(this object src) {
			List<string> rtnValue = new List<string>();						
#if NET_STD || NET_CORE || NET_STORE
			Type type = typeof(T);
			IEnumerable<PropertyInfo> properties = type.GetRuntimeProperties();
			foreach (PropertyInfo property in properties) {
				if (!property.CanRead) continue;
				rtnValue.Add(property.Name);
			}
#endif
#if NET_FX
			Type type = typeof(T);
			PropertyInfo[] properties = type.GetProperties();
			for (int cnti = 0; cnti < properties.Length; cnti++) {
				PropertyInfo property = properties[cnti];
				if (!property.CanRead) continue;
				rtnValue.Add(property.Name);
			}
#endif
			return rtnValue.ToArray();
		}
	}
}