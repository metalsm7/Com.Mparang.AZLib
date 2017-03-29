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
using System.Text;
using System.Collections.Generic;
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

        public AZString(object pString) { this.value_object = pString; }

        public static AZString Init(object pString) { return new AZString(pString); }

        public string String() { return String(""); }

        public string String(string pDefault) {
            string rtnValue = "";
            if (this.value_object == null) {
                rtnValue = pDefault;
            }
            else {
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
            }
            return rtnValue;
        }

        override public string ToString() { return String(); }

        public int ToInt() { return ToInt(0); }

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

        public static int ToInt(string pValue) { return AZString.ToInt(pValue, 0); }

        public long ToLong() { return ToLong(0); }

        public long ToLong(long pDefaultValue) {
            long rtnValue = 0;
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

        public static long ToLong(string pValue, long pDefaultValue) {
            long rtnValue = 0;
            try {
                rtnValue = Convert.ToInt32(pValue);
            }
            catch (Exception ex) {
                string msg = ex.Message;
                rtnValue = pDefaultValue;
            }
            return rtnValue;
        }

        public static long ToLong(string pValue) { return AZString.ToLong(pValue, 0); }

        public float ToFloat() { return ToFloat(0.0f); }

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

        public static float ToFloat(string pValue) { return AZString.ToFloat(pValue, 0.0f); }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        public DateTime ToDateTime() {
            return AZString.ToDateTime(this.Value);
        }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        public DateTime ToDateTime(DateTime? pDefaultValue) {
            return AZString.ToDateTime(this.Value, pDefaultValue);
        }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        public static DateTime ToDateTime(string pValue) {
            return ToDateTime(pValue, null);
        }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        public static DateTime ToDateTime(string pValue, DateTime? pDefaultValue) {
            DateTime rtnValue = new DateTime();
            if (pDefaultValue.HasValue) {
                rtnValue = pDefaultValue.Value;
            }
            try {
                rtnValue = DateTime.Parse(pValue.Trim());
            }
            catch (Exception) {
            }
            return rtnValue;
        }

        public static string Random() {
            int randomLength = 6;
            Random random = new Random(Guid.NewGuid().GetHashCode());
            random.Next();
            int rndValue = random.Next(12);
            if (rndValue >= 6) {
                rndValue = randomLength;
            }
            return AZString.Random(rndValue, RANDOM_TYPE.ALPHABET_AND_NUMBER, true);
        }

        public static string Random(int pLength) {
            return AZString.Random(pLength, RANDOM_TYPE.ALPHABET_AND_NUMBER, true);
        }


        /**
         * Created in 2015-07-02, leeyonghun
         */
        /*
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

        /**
         * <summary></summary>
         * Created in 2015-07-02, leeyonghun
         */
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
                    if (pCaseSensitive) {
                        sourceString += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    }
                    break;
            }
            return AZString.Init(sourceString).MakeRandom(pLength).String(); //AZString.Random(pLength, sourceString);
        }

        /**
         * <summary></summary>
         * Created in 2015-08-05, leeyonghun
         */
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

        public static string Repeat(string pString, int pLength) {
            StringBuilder rtnValue = new StringBuilder();
            for (int cnti = 0; cnti < pLength; cnti++) {
                rtnValue.Append(pString);
            }
            return rtnValue.ToString();
        }

        /**
         * <summary></summary>
         * Created in 2015-08-05, leeyonghun
         */
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

        /**
         * <summary></summary>
         * Created in 2017-02-23, leeyonghun
         */
        public static string Wrap(string pString, string pWrapString, bool pForce) {
            StringBuilder rtnValue = new StringBuilder();
            rtnValue.AppendFormat("{0}{1}{2}", !pForce && pString.StartsWith(pWrapString) ? "" : pWrapString, pString, !pForce && pString.EndsWith(pWrapString) ? "" : pWrapString);
            return rtnValue.ToString();
        }

        /**
         * <summary></summary>
         * Created in 2017-02-23, leeyonghun
         */
        public static string Wrap(string pString, string pWrapString) {
            return Wrap(pString, pWrapString, false);
        }

        /**
         * <summary></summary>
         * Created in 2017-02-23, leeyonghun
         */
        public AZString Wrap(string pWrapString, bool pForce) {
            this.value_object = AZString.Wrap(Value, pWrapString, pForce);
            return this;
        }

        /**
         * <summary></summary>
         * Created in 2017-02-23, leeyonghun
         */
        public AZString Wrap(string pWrapString) {
            this.value_object = AZString.Wrap(Value, pWrapString, false);
            return this;
        }

        /**
         * <summary></summary>
         * Created in 2015-08-05, leeyonghun
         */
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
                        if (!compString[cntk].Equals(dmyString)) {
                            break;
                        }
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

        public AZString Format(string pFormat, string pTarget) {
            this.value_object = AZString.Format(String(), pFormat, pTarget);
            return this;
        }

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

        public static String[] ToStringArray(string pSrc, int pLength) {
            return new AZString(pSrc).ToStringArray(pLength);
        }

        /**
         * <summary></summary>
         * Created in 2015-08-05, leeyonghun
         */
        public AZString Encode(ENCODE pEncode) {
            switch (pEncode) {
                case ENCODE.SQL:
                    this.value_object = ToSQLSafeEncoding();
                    break;
                case ENCODE.XSS:
                    this.value_object = ToXSSSafeEncoding();
                    break;
                case ENCODE.JSON:
                    this.value_object = ToJSONSafeEncoding();
                    break;
                case ENCODE.HTML:
                    this.value_object = ToHTMLSafeEncoding();
                    break;
                case ENCODE.XML:
                    this.value_object = ToXMLSafeEncoding();
                    break;
            }
            return this;
        }

        /**
         * <summary></summary>
         * Created in 2015-08-05, leeyonghun
         */
        public static string Encode(ENCODE pEncode, string pSrc) {
            return AZString.Init(pSrc).Encode(pEncode).Value;
        }

        /**
         * <summary></summary>
         * Created in 2015-08-05, leeyonghun
         */
        public AZString Decode(DECODE pDecode) {
            switch (pDecode) {
                case DECODE.SQL:
                    this.value_object = ToSQLSafeDecoding();
                    break;
                case DECODE.JSON:
                    this.value_object = ToJSONSafeDecoding();
                    break;
                case DECODE.HTML:
                    this.value_object = ToHTMLSafeDecoding();
                    break;
            }
            return this;
        }

        /**
         * <summary></summary>
         * Created in 2015-08-05, leeyonghun
         */
        public static string Decode(DECODE pDecode, string pSrc) {
            return AZString.Init(pSrc).Decode(pDecode).Value;
        }

        #region private encode/decode
        /**
         * <summary></summary>
         * Created in 2015-08-05, leeyonghun
         */
        private string ToXSSSafeEncoding() {
            string rtnValue;

            rtnValue = this.String();
            rtnValue = rtnValue.Replace("<s", "&lt;&#115;");
            rtnValue = rtnValue.Replace("<S", "&lt;&#83;");
            rtnValue = rtnValue.Replace("<i", "&lt;&#105;");
            rtnValue = rtnValue.Replace("<I", "&lt;&#73;");

            return rtnValue;
        }

        /**
         * <summary></summary>
         * Created in 2015-08-05, leeyonghun
         */
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

        /**
         * <summary></summary>
         * Created in 2015-08-05, leeyonghun
         */
        private string ToHTMLSafeEncoding() {
            string rtnValue;

            rtnValue = this.String();
            rtnValue = rtnValue.Replace("&", "&amp;");
            rtnValue = rtnValue.Replace("<", "&lt;");
            rtnValue = rtnValue.Replace(">", "&gt;");
            rtnValue = rtnValue.Replace("\"", "&quot;");

            return rtnValue;
        }

        /**
         * <summary></summary>
         * Created in 2015-08-05, leeyonghun
         */
        private string ToHTMLSafeDecoding() {
            string rtnValue;

            rtnValue = this.String();
            rtnValue = rtnValue.Replace("&lt;", "<");
            rtnValue = rtnValue.Replace("&gt;", ">");
            rtnValue = rtnValue.Replace("&quot;", "\"");
            rtnValue = rtnValue.Replace("&amp;", "&");

            return rtnValue;
        }

        private string ToSQLSafeEncoding() {
            string rtnValue;

            rtnValue = this.String();
            //rtnValue = rtnValue.Replace("&nbsp;", "&nbsp");
            rtnValue = rtnValue.Replace("#", "&#35;");      // # 위치 확인!
            rtnValue = rtnValue.Replace(";", "&#59;");
            rtnValue = rtnValue.Replace("'", "&#39;");
            rtnValue = rtnValue.Replace("--", "&#45;&#45;");
            rtnValue = rtnValue.Replace("\\", "&#92;");
            rtnValue = rtnValue.Replace("*", "&#42;");
            //rtnValue = rtnValue.Replace("&nbsp", "&nbsp;");

            return rtnValue;
        }

        /*
        private static string ToSQLSafeEncoding(string pSrc) {
            return new AZString(pSrc).ToSQLSafeEncoding();
        }
         */

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
        
        /*
        private static string ToSQLSafeDecoding(string pSrc) {
            return new AZString(pSrc).ToSQLSafeDecoding();
        }
         */

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
        /*
        private static string ToJSONSafeEncoding(string pSrc) {
            return new AZString(pSrc).ToJSONSafeEncoding();
        }
         */

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
        /*
        private static string ToJSONSafeDecoding(string pSrc) {
            return new AZString(pSrc).ToJSONSafeDecoding();
        }
         */
        #endregion

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        public T To<T>() {
            return AZString.To<T>(this.Value);
        }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
        public T To<T>(T pDefaultValue) {
            return AZString.To<T>(this.Value, pDefaultValue);
        }

        /**
         * <summary></summary>
         * Created in 2015-08-13, leeyonghun
         */
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

        /**
         * <summary></summary>
         * Created in 2015-08-04, leeyonghun
         */
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

        public class XML {
            private string xml;

            public XML(string p_xml) { this.xml = p_xml; }

            public static AZString.XML Init(string p_xml) { return new AZString.XML(p_xml); }

            private static string ReplaceInnerDQ(string pString, char p_replace_chr) { return ReplaceInner(pString, "\"", "\"", p_replace_chr); }
            private static string ReplaceInner(string pString, string pStartString, string pEndString, char pReplaceChr) {
                StringBuilder builder = new StringBuilder();

                int dqStartIdx = -1, dqEndIdx = -1, idx = 0, prevIdx = 0;
                int startCharExistAfterDQStart = 0, idxAfterDQStart = -1;   // appended
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

            //public AZData GetData() { return ToAZData(); }
            public AZData ToAZData() { return AZString.XML.ToAZData(this.xml); }
            //public static AZData GetData(string p_xml_string) { return ToAZData(p_xml_string); }
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
                    //Console.WriteLine("tag_name : " + tag_name);
                    //
                    rtnValue.Name = tag_name;

                    int idx_ender = rmDQStr.IndexOf("</" + tag_name + ">");

                    string tag_inner_string = "";
                    if (tag_inner_string_exist) {
                        tag_inner_string = p_xml_string.Substring(p_xml_string.IndexOf(" "), idx_closer - p_xml_string.IndexOf(" ")).Trim();
                    }
                    //Console.WriteLine("tag_inner_string : " + tag_inner_string);

                    string tag_inner_string_rpDQ = ReplaceInnerDQ(tag_inner_string, '_');
                    //Console.WriteLine("tag_inner_string_rpDQ : " + tag_inner_string_rpDQ);

                    int pre_idx = -1, start_idx = 0;
                    while (tag_inner_string_exist) {
                        if ((start_idx = tag_inner_string_rpDQ.IndexOf(" ", ++pre_idx)) > -1) {
                            //Console.WriteLine("tag_inner_string_rpDQ.index : " + pre_idx + " / " + start_idx);
                            string attribute = tag_inner_string.Substring(pre_idx, start_idx - pre_idx);
                            //Console.WriteLine("tag_inner_string_rpDQ.attribute : " + attribute);

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
                            //
                            //Console.WriteLine("tag_inner_string_rpDQ.attributes : " + attribute_name + ":" + attribute_value);
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
                            //
                            //Console.WriteLine("tag_inner_string_rpDQ.attributes.2 : " + attribute_name + ":" + attribute_value);
                            rtnValue.Attribute.Add(attribute_name, attribute_value);
                            break;
                        }
                        else {
                            break;
                        }
                    }

                    string tag_lower_string = p_xml_string.Substring(idx_closer + 1, idx_ender - (idx_closer + 1)).Trim();
                    //Console.WriteLine("tag_lower_string : " + tag_lower_string);

                    while (tag_lower_string.Trim().Length > 0) {

                        tag_lower_string = tag_lower_string.Trim();

                        if (tag_lower_string.StartsWith("<") && !tag_lower_string.StartsWith("<!")) {
                            //Console.WriteLine("if_1 tag_lower_string : " + tag_lower_string);

                            string inner_rmDQStr = ReplaceInnerDQ(tag_lower_string, '_');
                            int inner_idx_closer = inner_rmDQStr.IndexOf(">");
                            string inner_tag_name = "";
                            if (rmDQStr.IndexOf(" ") > idx_closer || rmDQStr.IndexOf(" ") < 0) {
                                inner_tag_name = tag_lower_string.Substring(1, inner_idx_closer - 1).Trim();
                            }
                            else {
                                inner_tag_name = tag_lower_string.Substring(1, inner_rmDQStr.IndexOf(" ") - 1).Trim();
                            }
                            //Console.WriteLine("inner_tag_name : " + inner_tag_name);


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

                            //Console.WriteLine("inner_data_string : " + inner_data_string);
                            //Console.WriteLine("tag_lower_string : " + tag_lower_string);

                            //
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
                            //Console.WriteLine("if_2 tag_lower_string : " + tag_lower_string);

                            string inner_rmDQStr = ReplaceInnerDQ(tag_lower_string, '_');
                            //Console.WriteLine("inner_rmDQStr : " + inner_rmDQStr);
                            string inner_rmCDataStr = ReplaceInner(inner_rmDQStr, "<![CDATA[", "]]>", '_');
                            //Console.WriteLine("inner_rmCDataStr : " + inner_rmCDataStr);

                            int inner_end_index = inner_rmCDataStr.IndexOf("<");
                            if (inner_end_index < 0) {
                                rtnValue.Value = tag_lower_string;
                                tag_lower_string = "";
                            }
                            else {
                                rtnValue.Value = tag_lower_string.Substring(0, inner_end_index);
                                tag_lower_string = tag_lower_string.Substring(inner_end_index);
                            }
                            //Console.WriteLine("value string : " + rtnValue.Value);
                        }
                    }

                    /*
                    Console.WriteLine("------------------------------------");
                    Console.WriteLine("list size : " + child.Size());
                    for (int cnti = 0; cnti < child.Size(); cnti++) {
                        Console.WriteLine("list #" + (cnti + 1) + " : " + child[cnti].ToXmlString());
                        for (int cntj = 0; cntj < child[cnti].Attribute.Size(); cntj++) {
                            Console.WriteLine("data #" + (cnti + 1) + ".Attribute #" + (cntj + 1) + " - " + child[cnti].Attribute.GetKey(cntj) + ":" + child[cnti].Attribute.Get(cntj));
                        }
                    }
                    */
                }

                return rtnValue;
            }
        }

        public class JSON {
            private string json;

            public JSON(string pJson) { this.json = pJson; }

            public static AZString.JSON Init(string pJson) { return new AZString.JSON(pJson); }

            /**
             * <summary>객체의 접근자가 public 인 property들에 대해 json 형식으로 변경 반환 처리</summary>
             * Created in 2015-07-27, leeyonghun
             */
            public static string Convert<T>(T pTarget) {
                return AZData.From<T>(pTarget).ToJsonString();
                /*AZData rtnValue = new AZData();

                Type type = typeof(T);
                //System.Reflection.PropertyInfo[] properties = type.GetProperties();
                IEnumerable<PropertyInfo> properties = type.GetRuntimeProperties();
                foreach (PropertyInfo property in properties) {
                    if (!property.CanRead) { continue; }
                    rtnValue.Add(property.Name, property.Name.Equals("SyncRoot") ? property.GetValue(pTarget, null).ToString() : property.GetValue(pTarget, null));
                }*/
                /*for (int cnti = 0; cnti < properties.Length; cnti++) {
                    System.Reflection.PropertyInfo property = properties[cnti];
                    if (!property.CanRead) {
                        continue;
                    }
                    // ICollection 구현체에 대한 재귀 오류 수정처리, 2016-05-19, 이용훈
                    rtnValue.Add(property.Name, property.Name.Equals("SyncRoot") ? property.GetValue(pTarget, null).ToString() : property.GetValue(pTarget, null));
                }*/

                //return rtnValue.ToJsonString();
            }

            private static string RemoveInnerDQ(string pString) { return RemoveInner(pString, '"', '"'); }

            private static string RemoveInner(string pString, char pStartChr, char pEndChr) {
                StringBuilder builder = new StringBuilder();

                int dqStartIdx = -1, dqEndIdx = -1, idx = 0, prevIdx = 0;
                int startCharExistAfterDQStart = 0, idxAfterDQStart = -1;   // appended
                bool inDQ = false;
                while (true) {
                    idx = inDQ ? pString.IndexOf(pEndChr, idx) : pString.IndexOf(pStartChr, idx);
                    if (idx > -1) {
                        if (idx == 0 || idx > 0 && pString[idx - 1] != '\\') {
                            if (inDQ) {

                                // appended
                                idxAfterDQStart = pString.IndexOf(pStartChr, startCharExistAfterDQStart == 0 ? (dqStartIdx + 1) : idxAfterDQStart + 1);
                                if (idxAfterDQStart > -1 && idxAfterDQStart < idx && pString[idxAfterDQStart - 1] != '\\') {
                                    startCharExistAfterDQStart = 1;
                                    idx += 1;
                                    continue;
                                }
                                // appended

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

            //public AZData GetData() { return ToAZData(); }
            public AZData ToAZData() { return AZString.JSON.ToAZData(this.json); }
            //public static AZData GetData(string pJsonString) { return ToAZData(pJsonString); }
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
                                //Console.WriteLine("ToAZData:" + valueString);

                                //
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
                                //Console.WriteLine("ToAZData:" + valueString);

                                //
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

            //public AZList GetList() { return ToAZList(); }
            public AZList ToAZList() { return AZString.JSON.ToAZList(this.json); }
            //public static AZList GetList(string pJsonString) { return ToAZList(pJsonString); }
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


    // 확장메소드
    public static class AZStringEx {
        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static string AZFormat(this string pSrc, string pFormat, string pTargetFormat) {
            return AZString.Format(pSrc, pFormat, pTargetFormat);
        }

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
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

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static string Format(this DateTime pDate) {
            return pDate.Format("YYYY-MM-DD hh:mm:ss");
        }

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static int ToInt(this string pSrc, int pDefault) {
            return AZString.ToInt(pSrc, pDefault);
        }

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static int ToInt(this string pSrc) {
            return AZString.ToInt(pSrc);
        }

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static long ToLong(this string pSrc, long pDefault) {
            return AZString.ToLong(pSrc, pDefault);
        }

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static long ToLong(this string pSrc) {
            return AZString.ToLong(pSrc);
        }

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static float ToFloat(this string pSrc, float pDefault) {
            return AZString.ToFloat(pSrc, pDefault);
        }

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static float ToFloat(this string pSrc) {
            return AZString.ToFloat(pSrc);
        }

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static string Random(this string pSrc, int pLength) {
            return AZString.Random(pLength, pSrc);
        }

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static string Repeat(this string pSrc, int pLength) {
            return AZString.Repeat(pSrc, pLength);
        }

        /**
         * <summary></summary>
         * Created in 2017-02-23, leeyonghun
         */
        public static string Wrap(this string pSrc, string pWrapString, bool pForce) {
            return AZString.Wrap(pSrc, pWrapString, pForce);
        }

        /**
         * <summary></summary>
         * Created in 2017-02-23, leeyonghun
         */
        public static string Wrap(this string pSrc, string pWrapString) {
            return AZString.Wrap(pSrc, pWrapString, false);
        }

        /**
         * <summary></summary>
         * Created in 2017-02-23, leeyonghun
         */
        public static string Join<T>(this T[] pSrc, string pSeperator) {
            if (pSrc.GetType() != typeof(int[]) && pSrc.GetType() != typeof(float[]) &&
                pSrc.GetType() != typeof(double[]) && pSrc.GetType() != typeof(string[])) {
                    throw new Exception("Only int, float, double, string type supported.");
            }
            StringBuilder rtnValue = new StringBuilder();
            for (int cnti=0; cnti<pSrc.Length; cnti++) {
                rtnValue.Append(pSrc[cnti]);
                if (cnti < pSrc.Length - 1 && pSeperator.Length > 0) {
                    rtnValue.Append(pSeperator);
                }
            }
            return rtnValue.ToString();
        }

        /**
         * <summary></summary>
         * Created in 2017-02-23, leeyonghun
         */
        public static string Join<T>(this T[] pSrc) {
            return Join(pSrc, "");
        }

        /**
         * <summary></summary>
         * Created in 2017-02-24, leeyonghun
         */
        public static bool Has<T>(this T[] pSrc, T pValue) {
            return IndexOf<T>(pSrc, pValue) > -1 ? true : false;
        }

        /**
         * <summary></summary>
         * Created in 2017-02-24, leeyonghun
         */
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

        /**
         * <summary></summary>
         * Created in 2017-02-23, leeyonghun
         * ex: arrs.Each(x => x.Wrap("'"));
         */
        public static T[] Each<T>(this T[] pSrc, Func<T, T> pFunc) {
            T[] rtnValue = pSrc;
            for (int cnti=0; cnti<rtnValue.Length; cnti++) {
                rtnValue[cnti] = pFunc(rtnValue[cnti]);
            }
            return rtnValue;
        }

        /**
         * <summary></summary>
         * Created in 2017-02-24, leeyonghun
         */
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

        /**
         * <summary></summary>
         * Created in 2017-02-24, leeyonghun
         */
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

        /**
         * <summary></summary>
         * Created in 2017-02-24, leeyonghun
         */
        public static List<T> ToList<T>(this T[] pSrc) {
            List<T> rtnValue = new List<T>();
            for (int cnti = 0; cnti<pSrc.Length; cnti++) {
                rtnValue.Add(pSrc[cnti]);
            }
            return rtnValue;
        }

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static string Encode(this string pSrc, AZString.ENCODE pEncode) {
            return AZString.Encode(pEncode, pSrc);
        }

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static string Decode(this string pSrc, AZString.DECODE pDecode) {
            return AZString.Decode(pDecode, pSrc);
        }

        /**
         * <summary></summary>
         * Created in 2016-08-19, leeyonghun
         */
        public static string Encrypt(this string pSrc, AZEncrypt.ENCRYPT pEncrypt, string pKey) {
            return pSrc.Encrypt(pEncrypt, pKey, null);
        }

        /**
         * <summary></summary>
         * Created in 2016-08-19, leeyonghun
         */
        public static string Encrypt(this string pSrc, AZEncrypt.ENCRYPT pEncrypt, string pKey, string pDefault) {
            string rtnValue = pDefault;
            switch (pEncrypt) {
                case AZEncrypt.ENCRYPT.AES256:
                    rtnValue = new AZEncrypt.AES256().Enc(pSrc, pKey);
                    break;
            } 
            return rtnValue;
        }

        /**
         * <summary></summary>
         * Created in 2016-08-19, leeyonghun
         */
        public static string Decrypt(this string pSrc, AZEncrypt.ENCRYPT pEncrypt, string pKey) {
            return pSrc.Decrypt(pEncrypt, pKey, null);
        }

        /**
         * <summary></summary>
         * Created in 2016-08-19, leeyonghun
         */
        public static string Decrypt(this string pSrc, AZEncrypt.ENCRYPT pEncrypt, string pKey, string pDefault) {
            string rtnValue = pDefault;
            switch (pEncrypt) {
                case AZEncrypt.ENCRYPT.AES256:
                    rtnValue = new AZEncrypt.AES256().Dec(pSrc, pKey);
                    break;
            } 
            return rtnValue;
        }

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static T To<T>(this object pSrc, float pDefault) {
            return AZString.To<T>(pSrc, pDefault);
        }

        /**
         * <summary></summary>
         * Created in 2016-02-23, leeyonghun
         */
        public static AZData ToAZData(this string pSrc) {
            return AZString.JSON.ToAZData(pSrc);
        }

        /**
         * <summary></summary>
         * Created in 2016-02-23, leeyonghun
         */
        public static AZList ToAZList(this string pSrc) {
            return AZString.JSON.ToAZList(pSrc);
        }

        /**
         * <summary></summary>
         * Created in 2016-02-15, leeyonghun
         */
        public static T To<T>(this object pSrc) {
            return AZString.To<T>(pSrc);
        }
    
        public static string ToJson<T>(this T pSrc) {
            return AZString.JSON.Convert(pSrc);
        }

        public static bool IsNumeric(this string s) {
            foreach (char c in s) {
                if (!char.IsDigit(c) && c != '.') {
                    return false;
                }
            }
            return true;
        }

        /// Created in 2017-03-29, leeyognhun
        public static string[] GetPropertyNames<T>(this object src) {
            List<string> rtnValue = new List<string>();            
#if NETCOREAPP1_0
            Type type = typeof(T);
            IEnumerable<PropertyInfo> properties = type.GetRuntimeProperties();
            foreach (PropertyInfo property in properties) {
                if (!property.CanRead) continue;
                rtnValue.Add(property.Name);
            }
#endif
#if NET40 || NET452
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