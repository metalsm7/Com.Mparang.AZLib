<?php
namespace AZLib {
  /**
   * AZData
   */
  class AZData implements \Iterator, \JsonSerializable, \ArrayAccess {
    // protected $CI;
    private $_key = 0;
    private $_keys = null;
    private $_data = null;
    public function __construct($json = null) {
      // $this->CI =& get_instance();
      //
      if (!is_null($json)) {
        $type = gettype($json);
        switch ($type) {
          case 'string':
            $this->_data = json_decode($json, true);
            break;
          case 'array':
            $this->_data = $json;
            break;
        }
        //
        switch ($type) {
          case 'string':
          case 'array':
            $this->_keys = array_keys($this->_data);
            break;
        }
      }
    }
  
    /**
     * 객체 생성 후 반환
     * @return AZData
     */
    public static function create(): AZData {
      return new AZData();
    }
  
    /**
     * string 형식의 json 문자열 또는 array 객체를 기반으로 자료 생성
     * @param string|array $json = null json 문자열 또는 array 객체
     * @return AZData
     */
    public static function parse($json = null): AZData {
      return new AZData($json);
    }
  
    /**
     * get, remove method에 대한 오버로딩 처리
     */
    public function __call($name, $args) {
      switch ($name) {
        case 'get':
          switch (gettype($args[0])) {
            case 'tinyint':
            case 'smallint':
            case 'mediumint':
            case 'integer':
            case 'bigint':
              return call_user_func_array(array($this, 'get_by_index'), $args);
            case 'string':
              return call_user_func_array(array($this, "get_by_key"), $args);
          }
          // no break
          case 'remove':
            switch (gettype($args[0])) {
              case 'tinyint':
              case 'smallint':
              case 'mediumint':
              case 'integer':
              case 'bigint':
                return call_user_func_array(array($this, 'remove_by_index'), $args);
              case 'string':
                return call_user_func_array(array($this, "remove_by_key"), $args);
            }
        break;
      }
    }
  
    // ArrayAccess 구현용
    public function offsetExists($offset) {
      switch (gettype($offset)) {
        case 'tinyint':
        case 'smallint':
        case 'mediumint':
        case 'integer':
        case 'bigint':
          return $offset > -1 && $offset < count($this->_data);
      }
      return $this->has_key($offset);
    }
  
    // ArrayAccess 구현용
    public function offsetGet($offset) {
      return $this->get($offset);
    }
  
    // ArrayAccess 구현용
    public function offsetSet($offset, $value) {
      $this->set($offset, $value);
    }
  
    // ArrayAccess 구현용
    public function offsetUnset($offset) {
      $this->remove($offset);
    }
      
    // iterator 구현용
    public function current() {
      return $this->get($this->_key);
    }
  
    // iterator 구현용
    public function key() {
      return $this->_key;
    }
  
    // iterator 구현용
    public function next() {
      ++$this->_key;
    }
  
    // iterator 구현용
    public function rewind() {
      $this->_key = 0;
    }
  
    // iterator 구현용
    public function valid() {
      return isset($this->_keys[$this->_key]);
    }
  
    /**
     * 지정된 키값이 정의되어 있는지 여부 반환
     * @param string $key 정의 여부 확인을 위한 키값
     * @return bool
     */
    public function has_key(string $key): bool {
      return array_key_exists($key, $this->_data) > 0;
      // return isset($this->_data[$key]);
    }
  
    /**
     * 현재 가지고 있는 전체 키 목록을 배열로 반환
     * @return array
     */
    public function keys() {
      return array_keys($this->_data);
    }
  
    /**
     * 지정된 index에 위치한 자료의 key 반환
     * @return string
     */
    public function get_key($idx): string {
      return array_keys($this->_data)[$idx];
    }
  
    /**
     * index값 기준 자료 반환
     * @param $idx index값
     * @return mixed
     */
    protected function get_by_index($idx) {
      if (is_null($this->_data)) return null;
      $cnt = count($this->_data);
      if ($idx < 0 || $idx >= $cnt) {
        return null;
      }
      return $this->_data[$this->_keys[$idx]];
    }
  
    /**
     * key값 기준 자료 반환
     * @param string $key key값
     * @return mixed
     */
    protected function get_by_key(string $key) {
      if (!$this->has_key($key)) {
        return null;
      }
      return $this->_data[$key];
    }
  
    public function size() {
      return count($this->_keys);
    }
  
    /**
     * 자료 추가
     * @param string $key 키값
     * @param mixed $value
     * @return AZData
     */
    public function add(string $key, $value) {
      if (is_null($this->_data)) {
        $this->_data = array();
        $this->_keys = array();
      }
      $this->_data[$key] = $value;
      array_push($this->_keys, $key);
      return $this;
    }
  
    public function set(string $key, $value) {
      if (!is_null($this->_data)) {
        if (!$this->has_key($key)) {
          $this->add($key, $value);
        }
        else {
          $this->_data[$key] = $value;
        }
      }
      return $this;
    }
  
    protected function remove_by_key(string $key) {
      $this->_data[$key] = null;
      unset($this->_data[$key]);
      return $this;
    }
  
    protected function remove_by_index($idx) {
      if ($idx > -1 && $idx < count($this->_data)) {
        $key = $this->_keys[$idx];
        array_splice($this->_keys, $idx, 1);
        reset($this->_keys);
        //
        $this->_data[$key] = null;
        unset($this->_data[$key]);
      }
      return $this;
    }
  
    public function convert($model) {
      if (gettype($model) === 'string') {
        $reflection = new ReflectionClass($model);
        $model = $reflection->newInstance();
      }
      $reflection = new ReflectionClass($model);
      $properties = $reflection->getProperties();
      foreach ($properties as $property) {
        $name = $property->getName();
        if ($this->has_key($name) && !$property->isStatic()) {
          $property->setAccessible(true);
          $property->setValue($model, $this->get($name));
          $property->setAccessible(false);
        }
      }
      return $model;
    }
  
    public function to_json() {
      $rtn_val = $this->_data;
      if (is_null($this->_data)) {
        $rtn_val = json_decode('{}');
      }
      else {
        $keys = array_keys($rtn_val);
        for ($i = 0; $i < count($keys); $i++) {
          if ($rtn_val[$keys[$i]] instanceof AZData) {
            $rtn_val[$keys[$i]] = $rtn_val[$keys[$i]]->to_json();
          } elseif ($rtn_val[$keys[$i]] instanceof AZList) {
            $rtn_val[$keys[$i]] = $rtn_val[$keys[$i]]->to_json();
          }
        }
      }
      return $rtn_val;
    }
  
    public function to_json_string(): string {
      return json_encode($this->to_json());
    }
  
    public function jsonSerialize(): string {
      return $this->to_json_string();
    }
  }

  class AZList implements \Iterator, \JsonSerializable {
    // protected $CI;
    private $_key = 0;
    private $_data = array();
    public function __construct(string $json = null) {
      // $this->CI =& get_instance();
      //
      if (!is_null($json)) {
        $this->_data = json_decode($json, true);
      }
    }
  
    public static function create(): AZList {
      return new AZList();
    }
  
    public static function parse(string $json = null): AZList {
      return new AZList($json);
    }
  
    // iterator 구현용
    public function current() {
      return $this->_data[$this->_key];
    }
  
    // iterator 구현용
    public function key() {
      return $this->_key;
    }
  
    // iterator 구현용
    public function next() {
      ++$this->_key;
    }
  
    // iterator 구현용
    public function rewind() {
      $this->_key = 0;
    }
  
    // iterator 구현용
    public function valid() {
      return isset($this->_data[$this->_key]);
    }
  
    public function size() {
      if (is_null($this->_data)) {
        return 0;
      }
      return count($this->_data);
    }
  
    public function get(int $idx) {
      if ($this->size() >= $idx + 1) {
        return $this->_data[$idx];
      }
      return null;
    }
  
    public function add(AZData $data) {
      array_push($this->_data, $data);
      return $this;
    }
  
    public function remove(int $idx) {
      if ($this->size() >= $idx + 1) {
        unset($this->_data[$idx]);
      }
      return $this;
    }
  
    public function push(AZData $data) {
      array_push($this->_data, $data);
      return $this;
    }
  
    public function pop() {
      return array_pop($this->_data);
    }
  
    public function shift() {
      return array_shift($this->_data);
    }
  
    public function unshift(AZData $data) {
      array_unshift($this->_data, $data);
      return $this;
    }
  
    public function convert($model): array {
      $rtn_val = array();
      foreach ($this->_data as $data) {
        array_push($rtn_val, $data->convert($model));
      }
      return $rtn_val;
    }
  
    public function to_json() {
      $rtn_val = $this->_data;
      if (!is_null($rtn_val)) {
        for ($i = 0; $i < count($rtn_val); $i++) {
          if ($rtn_val[$i] instanceof AZData) {
            $rtn_val[$i] = $rtn_val[$i]->to_json();
          }
        }
      }
      return $rtn_val;
    }
  
    public function to_json_string(): string {
      return json_encode($this->to_json());
    }
  
    public function jsonSerialize(): string {
      return $this->to_json_string();
    }
  }

  class AZSql {
    //
    private $_db; // string
    private $_query; // string
    private $_compiled_query; // 실제 요청시 사용될 쿼리문
    private $_parameters; // AZData
    private $_prepared_parameter_types; // array, prepared statement 사용시 사용될 parameters 의 type 목록
    private $_prepared_parameter_keys; // array, prepared statement 사용시 사용될 parameters 의 key 목록
    private $_return_parameters; // AZData, out 반환값 설정 자료
    private $_identity = false;
    private $_is_transaction = false;
    private $_transaction_result;
    private $_action_tran_on_commit;
    private $_action_tran_on_rollback;
    private $_is_stored_procedure = false;
    private $_is_prepared = false;
    private $_statement; // prepared statement 사용시 prepare() 결과 객체 저장용
    //
    public function __construct(&$db = null) {
      //
      if (!is_null($db)) {
        $this->_db = $db;
      }
      //
      $this->_parameters = AZData::create();
    }

    function __destruct() {
      $this->clear();
    }

    public static function create(&$db = null) {
      return new AZSql($db);
    }

    /**
     * 연결 객체에 대한 class 명을 통해 mysqli 여부 반환, CI에서 사용을 위한 conn_id 확인
     * @return boolean
     */
    protected function is_mysqli() {
      if (!isset($this->_db)) {
        throw new \Exception('database object is not defined');
      }
      if (isset($this->_db->conn_id)) {
        return get_class($this->_db->conn_id) == 'mysqli';
      }
      return get_class($this->_db) == 'mysqli';
    }
    
    /**
     * mysqli 객체를 반환
     */
    protected function get_mysqli() {
      return $this->is_mysqli() ? (isset($this->_db->conn_id) ? $this->_db->conn_id : $this->_db ) : null;
    }

    /**
     * 쿼리 생성을 위한 인수값에 대한 변경 처리, prepared statement를 사용하지 않는 경우 처리
     * @param $query string 변경할 쿼리 문자열
     * @param $key string
     * @param $value mixed 
     * @return string
     */
    private function param_replacer(string $query, string $key, $value) {
      if (is_null($query) || !is_null($query) && strlen($query) < 1) throw new \Exception('query is empty');
      $query = preg_replace("/{$key}$/", $this->get_mysqli()->escape_string($value), $query);
      $query = preg_replace("/{$key}\r\n/", $this->get_mysqli()->escape_string($value)."\r\n", $query);
      $query = preg_replace("/{$key}\n/", $this->get_mysqli()->escape_string($value)."\n", $query);
      $query = preg_replace("/{$key}\s/", $this->get_mysqli()->escape_string($value)." ", $query);
      $query = preg_replace("/{$key}\t/", $this->get_mysqli()->escape_string($value)."\t", $query);
      $query = preg_replace("/{$key},/", $this->get_mysqli()->escape_string($value).",", $query);
      $query = preg_replace("/{$key}\)/", $this->get_mysqli()->escape_string($value).")", $query);
      $query = preg_replace("/{$key};/", $this->get_mysqli()->escape_string($value).";", $query);
      return $query;
    }

    /*
     * 특정 양식의 쿼리문에 대해 prepared statement 사용을 위한 전달값 생성 및 반환
     *
    private function prepared_query_param_replacer(string &$query, $data) {
      // 반환자료 생성
      $rtn_val = array('types' => array(), 'values' => array());
      //
      foreach ($data as $key => $value) {
        $replace_cnt = 0;
        $query = preg_replace_callback("/{$key}$/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'; }, $query);
        $query = preg_replace_callback("/{$key}\r\n/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'."\r\n"; }, $query);
        $query = preg_replace_callback("/{$key}\n/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'."\n"; }, $query);
        $query = preg_replace_callback("/{$key}\s/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'." "; }, $query);
        $query = preg_replace_callback("/{$key}\t/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'."\t"; }, $query);
        $query = preg_replace_callback("/{$key},/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'.","; }, $query);
        $query = preg_replace_callback("/{$key}\)/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'.")"; }, $query);
        $query = preg_replace_callback("/{$key};/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'.";"; }, $query);
        //
        for ($i = 0; $i < $replace_cnt; $i++) {
          $type_str = 's';
          switch (gettype($data[$key])) {
            case 'integer':
              $type_str = 'i';
              break;
            case 'float':
            case 'double':
              $type_str = 'd';
              break;
            default:
              $type_str = 's';
              break;
          }
          array_push($rtn_val['types'], $type_str);
          array_push($rtn_val['values'], $data[$key]);
        }
      }
      return $rtn_val;
    }
    */

    /**
     * 특정 양식의 쿼리문에 대해 prepared statement 사용을 위한 쿼리문으로 변경 처리.
     */
    private function prepared_query_replacer(string &$query, $data) {
      // 반환자료 생성
      $rtn_val = array('types' => array(), 'keys' => array());
      if (strlen($query) < 1 || count($data) < 1) return $rtn_val;
      //
      $reg_str = "/(".join(')|(', array_keys($data)).")/";
      preg_match_all($reg_str, $query, $matches, PREG_PATTERN_ORDER);
      foreach ($matches[0] as $key) {
        $replace_cnt = 0;
        $query = preg_replace_callback("/{$key}$/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'; }, $query, 1);
        if ($replace_cnt < 1) $query = preg_replace_callback("/{$key}\r\n/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'."\r\n"; }, $query, 1);
        if ($replace_cnt < 1) $query = preg_replace_callback("/{$key}\n/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'."\n"; }, $query, 1);
        if ($replace_cnt < 1) $query = preg_replace_callback("/{$key}\s/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'." "; }, $query, 1);
        if ($replace_cnt < 1) $query = preg_replace_callback("/{$key}\t/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'."\t"; }, $query, 1);
        if ($replace_cnt < 1) $query = preg_replace_callback("/{$key},/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'.","; }, $query, 1);
        if ($replace_cnt < 1) $query = preg_replace_callback("/{$key}\)/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'.")"; }, $query, 1);
        if ($replace_cnt < 1) $query = preg_replace_callback("/{$key};/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'.";"; }, $query, 1);
        //
        for ($i = 0; $i < $replace_cnt; $i++) {
          $type_str = 's';
          switch (gettype($data[$key])) {
            case 'integer':
              $type_str = 'i';
              break;
            case 'float':
            case 'double':
              $type_str = 'd';
              break;
            default:
              $type_str = 's';
              break;
          }
          array_push($rtn_val['types'], $type_str);
          array_push($rtn_val['keys'], $key);
        }
      }
      return $rtn_val;
    }

    /**
     * prepared statement 를 사용하지 않은 경우에 한하여, 
     * 요청시 컬럼정보의 type에 따른 반환값에 대한 타입 캐스팅 처리.
     */
    private function value_caster(&$value, $type) {
      switch ($type) {
        case 1:
        case 2:
        case 3:
        case 8:
        case 9:
        case 16:
          $value = intval($value);
          break;
        case 4:
          $value = floatval($value);
          break;
        case 5:
        case 246:
          $value = doubleval($value);
          break;
      }
    }

    /**
     * prepared statement 를 사용하지 않은 경우에 한하여, 
     * 요청시 컬럼정보의 type에 따른 반환값에 대한 타입 캐스팅 처리.
     */
    private function data_value_caster(array &$data, $metadata) {
      foreach ($data as $key => &$value) {
        foreach ($metadata as $meta) {
          if ($meta->name === $key) {
            $this->value_caster($value, $meta->type);
            break;
          }
        }
      }
    }

    /**
     * method overloading 을 위한 처리(magic method).
     */
    public function __call($name, $args) {
      switch ($name) {
        case 'execute':
        case 'get':
        case 'get_data':
        case 'get_list':
        case 'get_multi':
          switch (count($args)) {
            case 0:
              return call_user_func_array(array($this, $name), array(false));
            case 1:
              return call_user_func_array(array($this, $name), $args);
            case 2:
              if (gettype($args[1]) === 'boolean') {
                return call_user_func_array(array($this, "{$name}_with_query"), $args);
              } else {
                return call_user_func_array(array($this, "{$name}_with_params"), $args);
              }
              // no break
            case 3:
              return call_user_func_array(array($this, "{$name}_with_params"), $args);
          }
          break;
        case 'add_return_parameter':
          if (count($args) == 1) array_push($args, null);
          return call_user_func_array(array($this, $name), $args);
      }
    }

    /**
     * 실행할 쿼리문 설정.
     * 현재 설정된 DB드라이버에 맞게 변경된 쿼리문과
     * prepared statement 사용을 위한 전달자료도 같이 초기화 처리.
     * @return AZSql
     */
    public function set_query($query) {
      //
      $this->_query = $query;
      $this->_compiled_query = null;
      //
      $this->_prepared_parameter_types = null;
      $this->_prepared_parameter_keys = null;
      //
      return $this;
    }

    /**
     * set_query()로 설정된 쿼리문 반환.
     */
    public function get_query(): string {
      return $this->_query;
    }

    /**
     * set_query()로 설정된 쿼리문 초기화.
     * 현재 설정된 DB드라이버에 맞게 변경된 쿼리문과
     * prepared statement 사용을 위한 전달자료도 같이 초기화 처리.
     * @return AZSql
     */
    public function clear_query() {
      //
      $this->_query = null;
      $this->_compiled_query = null;
      //
      $this->_prepared_parameter_types = null;
      $this->_prepared_parameter_keys = null;
      //
      return $this;
    }

    /*
    public function get_compiled_query(): string {
      if ($this->is_prepared()) {
        $this->compile_prepared();
      }
      else {
        $this->compile();
      }
      return $this->_compiled_query;
    }
    */

    /**
     * 현재 사용중인 DB드라이버에 맞게 변경된 쿼리문 생성
     * @return AZSql
     */
    protected function compile() {
      if ($this->_compiled_query) return;
      //
      $query = $this->get_query();
      $params = $this->get_parameters();
      if (gettype($params) === 'object' && $params instanceof AZData) {
        $params = $params->to_json();
      }
      foreach ($params as $key => $value) {
        $query = $this->param_replacer($query, $key, $value);
      }
      //
      $this->_compiled_query = $query;
      //
      return $this;
    }

    /**
     * 현재 사용중인 DB드라이버에 맞게
     * prepared statement 사용을 위한 변경된 쿼리문과
     * 전달 인자 생성 처리.
     */
    protected function compile_prepared() {
      //
      if (
          !$this->_compiled_query ||
          !$this->_prepared_parameter_types ||
          !$this->_prepared_parameter_keys
        ) {
        $query = $this->get_query();
        $params = $this->get_parameters();
        if (gettype($params) === 'object' && $params instanceof AZData) {
          $params = $params->to_json();
        }
        $result = $this->prepared_query_replacer($query, $params);
        $this->_compiled_query = $query;
        $this->_prepared_parameter_types = $result['types'];
        $this->_prepared_parameter_keys = $result['keys'];
        //
        $this->_statement = null;
        //
        return true;
      }
      //
      return false;
    }

    /**
     * 쿼리 전달값 설정.
     * @param $param AZData|array array 사용시 내부적으로 AZData로 변경 처리
     * @return AZSql
     */
    public function set_parameters($params) {
    // public function set_parameters(&$params) {
      if (gettype($params) === 'array') {
        $this->_parameters = AZData::parse($params);
      }
      else {
        $this->_parameters = $params;
      }
      /*
      if (gettype($params) === 'object' && $params instanceof AZData) {
        $this->_parameters = $params->to_json();
      }
      */
      return $this;
    }

    /**
     * 쿼리 전달값 반환.
     * @return AZData
     */
    public function get_parameters() {
      return $this->_parameters;
    }

    /**
     * $key 값으로 특정된 쿼리 전달값 반환.
     * @param $key string 키값
     * @return mixed
     */
    public function get_parameter(string $key) {
      return $this->_parameters->get($key);
    }

    /**
     * 쿼리 전달값 개별 추가 처리.
     * @param $key string 키값
     * @param $value mixed 키값에 1:1로 대응하는 값
     * @return AZSql
     */
    public function add_parameter(string $key, $value) {
      if (!$this->is_prepared()) {
        $this->_compiled_query = null;
        $this->_statement = null;
      }
      $this->_parameters->add($key, $value);
      return $this;
    }

    /**
     * $key 값으로 특정된 쿼리 전달값 개별 삭제 처리
     * @param $key string 키값
     * @return AZSql
     */
    public function remove_parameter(string $key) {
      if (!$this->is_prepared()) {
        $this->_compiled_query = null;
        $this->_statement = null;
      }
      $this->_parameters->remove($key);
      return $this;
    }

    /**
     * 쿼리 전달값 초기화.
     * 현재 설정된 DB드라이버에 맞게 변경된 쿼리문과
     * 생성된 prepared statement 객체도 초기화.
     * @return AZSql
     */
    public function clear_parameters() {
      if (!$this->is_prepared()) {
        $this->_compiled_query = null;
        $this->_statement = null;
      }
      $this->_parameters->clear();
      return $this;
    }

    /**
     * stored procedure 실행 후 반환값을 받기 위한 반환값 설정.
     * @param &$params AZData
     * @return AZSql
     */
    public function set_return_parameters(AZData &$params) {
      $this->_return_parameters = $params;
      return $this;
    }

    /**
     * 쿼리 반환값 반환.
     * @return AZData
     */
    public function get_return_parameters() {
      return $this->_return_parameters;
    }

    /**
     * $key로 특정되는 쿼리 반환값 반환.
     * @param $key string
     * @return mixed
     */
    public function get_return_parameter(string $key) {
      return $this->_return_parameters->get($key);
    }

    /**
     * 쿼리 반환값 개별 추가.
     * @param $key string 키값
     * @param $value 생략가능, 반환값이 발생하는 경우 해당 값으로 변경
     * @return AZSql
     */
    public function add_return_parameter(string $key, $value = null) {
      if (!$this->is_prepared()) {
        $this->_compiled_query = null;
        $this->_statement = null;
      }
      if (is_null($this->_return_parameters)) {
        $this->_return_parameters = AZData::create();
      }
      $this->_return_parameters->add($key, $value);
      return $this;
    }

    /**
     * $key로 특정되는 쿼리 반환값의 개별값 수정.
     * @param $key string 키값
     * @param $value 변경할 값
     * @return AZSql
     */
    public function update_return_parameter(string $key, $value) {
      if (!is_null($this->_return_parameters)) {
        $this->_return_parameters->set($key, $value);
      }
      return $this;
    }

    /**
     * $key로 특정되는 쿼리 반환값 삭제.
     * @param $key string 키값
     * @return AZSql
     */
    public function remove_return_parameter(string $key) {
      if (!$this->is_prepared()) {
        $this->_compiled_query = null;
        $this->_statement = null;
      }
      if (is_null($this->_return_parameters)) {
        $this->_return_parameters->remove($key);
      }
      return $this;
    }

    /**
     * 쿼리 반환값 초기화.
     * @return AZSql
     */
    public function clear_return_parameters() {
      if (!$this->is_prepared()) {
        $this->_compiled_query = null;
        $this->_statement = null;
      }
      $this->_return_parameters = null;
      return $this;
    }

    /**
     * prepared statement 사용 여부 설정
     * @param $state boolean
     * @return AZSql
     */
    public function set_prepared($state) {
      //
      $this->_is_prepared = $state;
      //
      $this->_compiled_query = null;
      //
      $this->_statement = null;
      //
      $this->_prepared_parameter_types = null;
      $this->_prepared_parameter_keys = null;
      //
      return $this;
    }

    /**
     * prepared statement 사용여부 반환
     * @return boolean
     */
    public function is_prepared() {
      return $this->_is_prepared;
    }

    /**
     * stored procedure 사용 여부 설정
     * @param $state boolean
     * @return AZSql
     */
    public function set_stored_procedure($state) {
      $this->_is_stored_procedure = $state;
      return $this;
    }

    /**
     * stored procedure 사용 여부 반환
     * @return boolean
     */
    public function is_stored_procedure() {
      return $this->_is_stored_procedure;
    }

    /**
     * 모든 설정 자료 초기화.
     * @return AZSql
     */
    public function clear() {
      //
      $this->clear_query();
      $this->clear_parameters();
      $this->clear_return_parameters();
      //
      $this->_identity = false;
      $this->_is_transaction = false;
      $this->_transaction_result;
      $this->_action_tran_on_commit;
      $this->_action_tran_on_rollback;
      $this->_is_stored_procedure = false;
      $this->_is_prepared = false;
      //
      return $this;
    }

    /**
     * transaction 사용
     * @param @on_commit callable 커밋 성공시 처리 함수
     * @param @on_rollback callable 커밋 실패시 rollback 처리 후 처리 함수
     */
    public function begin_tran(callable $on_commit, callable $on_rollback) {
      $this->get_mysqli()->autocommit(false);
      //
      $this->_is_transaction = true;
      //
      if ($on_commit) $this->_action_tran_on_commit = $on_commit;
      if ($on_rollback) $this->_action_tran_on_rollback = $on_rollback;
    }

    /**
     * transaction commit.
     */
    public function commit() {
      $is_success = true;
      try {
        $this->get_mysqli()->commit();
      }
      catch (Exception $ex) {
        $this->get_mysqli()->rollback();
        $is_success = false;
      }
      finally {
        $this->get_mysqli()->autocommit(true);
      }
      //
      if ($is_success && $_action_tran_on_commit) {
        $_action_tran_on_commit();
      }
      else if (!$is_success && $_action_tran_on_rollback) {
        $_action_tran_on_commit();
      }
    }

    /**
     * 결과값이 필요하지 않는 단순 쿼리 실행시 사용.
     * @param $identity boolean = false insert 처리에 한하여 생성된 id값 반환 요청
     * @return int $identity가 참이며 id값이 생성된 경우 생성된 id값을, 그 외의 경우는 쿼리에 대해 적용받은 행의 갯수
     */
    protected function execute(bool $identity = false): int {
      //
      $rtn_val = 0;
      //
      if ($this->is_prepared()) {
        // prepared statement 사용의 경우

        $this->compile_prepared();
        //
        if (is_null($this->_statement)) {
          $this->_statement = $this->get_mysqli()->prepare($this->_compiled_query);
        }
        if (!$this->_statement) {
          // throw new \Exception($this->_db->error()['message'], $this->_db->error()['code']);
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        if ($this->_statement && $this->get_mysqli()->error && $this->get_mysqli()->errno != 0) {
          // throw new \Exception($this->_db->error()['message'], $this->_db->error()['code']);
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        //
        if (
            $this->get_parameters() && 
            $this->get_parameters()->size() > 0 &&
            $this->_prepared_parameter_types && 
            $this->_prepared_parameter_keys
          ) {
          $types = join('', $this->_prepared_parameter_types);
          $values = array();
          foreach ($this->_prepared_parameter_keys as $key) {
            array_push($values, $this->get_parameter($key));
          }
          $this->_statement->bind_param($types, ...$values);
        }
        //
        if ($this->_statement->execute()) {
          if (gettype($result) == 'object') {
            $result->free_result();
          }
          //
          $rtn_val = $identity ? $this->_statement->insert_id() : $this->_statement->affected_rows();
          //
          // $idx = 0;
          while ($this->_statement->more_results() && $this->_statement->next_result()) {
            if ($result = $this->_statement->get_result()) {
              $result->free_result();
            }
          }
          //
          $this->_statement->free_result();
          // $this->_statement->close();
        }
      }
      else {
        // prepared statement 가 아닌 일반 사용의 경우

        $this->compile();
        //
        $result = $this->get_mysqli()->query($this->_compiled_query);
        if (!$result) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        if ($result && $this->get_mysqli()->error && $this->get_mysqli()->errno != 0) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        $rtn_val = $identity ? $this->get_mysqli()->insert_id() : $this->get_mysqli()->affected_rows();
        //
        if (gettype($result) == 'object') {
          $this->free_results($result);
          $result->free_result();
        }
      }
      //
      return $rtn_val;
    }

    /**
     * execute() overload
     */
    protected function execute_with_query(string $query, bool $identity = false): int {
      //
      $this->set_query($query);
      $this->clear_parameters();
      //
      return $this->execute($identity);
    }

    /**
     * execute() overload
     */
    protected function execute_with_params(string $query, $params, bool $identity = false): int {
      //
      $this->set_query($query);
      $this->set_parameters($params);
      //
      return $this->execute($identity);
    }

    protected function get($type_cast = false) {
      //
      return $this->get_data($type_cast)->get(0);
    }

    protected function get_with_query(string $query, $type_cast = false) {
      //
      $this->set_query($query);
      $this->clear_parameters();
      //
      return $this->get($type_cast);
    }

    protected function get_with_params(string $query, $params, $type_cast = false) {
      //
      $this->set_query($query);
      $this->set_parameters($params);
      //
      return $this->get($type_cast);
    }

    protected function get_data($type_cast = false): AZData {
      //
      $rtn_val = AZData::create();
      //
      if ($this->is_prepared()) {
        // prepared statement 사용의 경우

        $this->compile_prepared();
        //
        if (is_null($this->_statement)) {
          $this->_statement = $this->get_mysqli()->prepare($this->_compiled_query);
        }
        if (!$this->_statement) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        if ($this->_statement && $this->get_mysqli()->error && $this->get_mysqli()->errno != 0) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        //
        if (
            $this->get_parameters() && 
            $this->get_parameters()->size() > 0 &&
            $this->_prepared_parameter_types && 
            $this->_prepared_parameter_keys
          ) {
          $types = join('', $this->_prepared_parameter_types);
          $values = array();
          foreach ($this->_prepared_parameter_keys as $key) {
            array_push($values, $this->get_parameter($key));
          }
          $this->_statement->bind_param($types, ...$values);
        }
        //
        if ($this->_statement->execute()) {
          $result = $this->_statement->get_result();
          $fields = array();
          while ($field = $result->fetch_field()) {
            array_push($fields, $field->name);
          }
          while ($row = $result->fetch_array(MYSQLI_NUM)) {
            //
            $idx = 0;
            foreach ($fields as $field) {
              $rtn_val->add($field, $row[$idx]);
              $idx++;
            }
          }
          $result->free_result();
          //
          while ($this->_statement->more_results() && $this->_statement->next_result()) {
            if ($result = $this->_statement->get_result()) {
              $result->free_result();
            }
          }
          //
          $this->_statement->free_result();
          // $this->_statement->close();
        }
      }
      else {
        // prepared statement 가 아닌 일반 사용의 경우

        $this->compile();
        //
        $result = $this->get_mysqli()->query($this->_compiled_query);
        if (!$result) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        if ($result && $this->get_mysqli()->error && $this->get_mysqli()->errno != 0) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        $data = $result->fetch_array(MYSQLI_ASSOC);
        if ($type_cast) {
          $field = $result->field_data();
          $this->data_value_caster($data, $field);
          unset($field);
        }
        //
        $rtn_val = AZData::parse($data);
        //
        unset($params);
        unset($type_cast);
        $this->free_results($result);
        $result->free_result();
      }

      //
      if (
          $this->is_stored_procedure() &&
          !is_null($this->get_return_parameters()) &&
          $this->get_return_parameters()->size() > 0
        ) {
        //
        $query = 'SELECT';
        //
        $idx = 0;
        $keys = $this->get_return_parameters()->keys();
        foreach ($keys as $key) {
          $query .= ($idx > 0 ? ', ' : ' ').$key.' AS '.str_replace('@', 'o_', $key);
          $idx++;
        }
        $result = $this->get_mysqli()->query($query);
        if ($result && $result->num_rows > 0) {
          //
          $fields = array();
          while ($field = $result->fetch_field()) {
            $fields[$field->name] = $field->type;
          }
          //
          $result->data_seek(0);
          //
          $data = $result->fetch_array(MYSQLI_ASSOC);
          $result->free_result();
          //
          foreach ($keys as $key) {
            $key_mod = str_replace('@', 'o_', $key);
            $value = $data[$key_mod];
            $this->value_caster($value, $fields[$key_mod]);
            $this->update_return_parameter($key, $value);
          }
          //
          $data = null;
        }
      }
      //
      return $rtn_val;
    }

    /**
     * 지정된 쿼리 문자열에 대한 단일 행 결과를 AZData 객체로 반환
     * @param string $query 쿼리 문자열
     * @param bool $type_cast = false 결과값을 DB의 자료형에 맞춰서 type casting 할지 여부
     * @return AZData
     */
    protected function get_data_with_query(string $query, $type_cast = false): AZData {
      //
      $this->set_query($query);
      $this->clear_parameters();
      //
      return $this->get_data($type_cast);
    }

    /**
     * 지정된 쿼리 문자열에 대한 단일 행 결과를 AZData 객체로 반환
     * @param string $query 쿼리 문자열
     * @param AZData|array $params 쿼리 문자열에 등록된 대체 문자열 자료
     * @param bool $type_cast = false 결과값을 DB의 자료형에 맞춰서 type casting 할지 여부
     * @return AZData
     */
    protected function get_data_with_params(string $query, $params, $type_cast = false): AZData {
      //
      $this->set_query($query);
      $this->set_parameters($params);
      //
      return $this->get_data($type_cast);
    }

    /**
     * 지정된 쿼리 문자열에 대한 다행 결과를 AZList 객체로 반환
     */
    protected function get_list($type_cast = false): AZList {
      $rtn_val = AZList::create();
      //
      if ($this->is_prepared()) {
        // prepared statement 사용의 경우

        $this->compile_prepared();
        //
        if (is_null($this->_statement)) {
          $this->_statement = $this->get_mysqli()->prepare($this->_compiled_query);
        }
        if (!$this->_statement) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        if ($this->_statement && $this->get_mysqli()->error && $this->get_mysqli()->errno != 0) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        //
        if (
            $this->get_parameters() && 
            $this->get_parameters()->size() > 0 &&
            $this->_prepared_parameter_types && 
            $this->_prepared_parameter_keys
          ) {
          $types = join('', $this->_prepared_parameter_types);
          $values = array();
          foreach ($this->_prepared_parameter_keys as $key) {
            array_push($values, $this->get_parameter($key));
          }
          $this->_statement->bind_param($types, ...$values);
        }
        //
        if ($this->_statement->execute()) {
          $result = $this->_statement->get_result();
          $fields = array();
          while ($field = $result->fetch_field()) {
            array_push($fields, $field->name);
          }
          while ($row = $result->fetch_array(MYSQLI_NUM)) {
            $rtn_data = AZData::create();
            //
            $idx = 0;
            foreach ($fields as $field) {
              $rtn_data->add($field, $row[$idx]);
              $idx++;
            }
            //
            $rtn_val->add($rtn_data);
          }
          $result->free_result();
          //
          while ($this->_statement->more_results() && $this->_statement->next_result()) {
            if ($result = $this->_statement->get_result()) {
              $result->free_result();
            }
          }
          //
          $this->_statement->free_result();
          // $this->_statement->close();
        }
      }
      else {
        // prepared statement 가 아닌 일반 사용의 경우

        $this->compile();
        //
        $result = $this->get_mysqli()->query($this->_compiled_query);
        if (!$result) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        if ($result && $this->get_mysqli()->error && $this->get_mysqli()->errno != 0) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        // $list = $result->result_array();
        // $result = $this->get_mysqli()->get_result();
        $list = $result->fetch_all(MYSQLI_ASSOC);
        $field = null;
        if ($type_cast) {
          $field = $result->field_data();
        }
        foreach ($list as $data) {
          if ($type_cast) {
            $this->data_value_caster($data, $field);
          }
          $rtn_val->add(AZData::parse($data));
        }
        //
        unset($field);
        unset($type_cast);
        $this->free_results($result);
        $result->free_result();
      }

      //
      if (
          $this->is_stored_procedure() &&
          !is_null($this->get_return_parameters()) &&
          $this->get_return_parameters()->size() > 0
        ) {
        //
        $query = 'SELECT';
        //
        $idx = 0;
        $keys = $this->get_return_parameters()->keys();
        foreach ($keys as $key) {
          $query .= ($idx > 0 ? ', ' : ' ').$key.' AS '.str_replace('@', 'o_', $key);
          $idx++;
        }
        $result = $this->get_mysqli()->query($query);
        if ($result && $result->num_rows > 0) {
          //
          $fields = array();
          while ($field = $result->fetch_field()) {
            $fields[$field->name] = $field->type;
          }
          //
          $result->data_seek(0);
          //
          $data = $result->fetch_array(MYSQLI_ASSOC);
          $result->free_result();
          //
          foreach ($keys as $key) {
            $key_mod = str_replace('@', 'o_', $key);
            $value = $data[$key_mod];
            $this->value_caster($value, $fields[$key_mod]);
            $this->update_return_parameter($key, $value);
          }
          //
          $data = null;
        }
      }
      //
      return $rtn_val;
    }
    
    protected function get_list_with_query(string $query, $type_cast = false): AZList {
      //
      $this->set_query($query);
      $this->clear_parameters();
      //
      return $this->get_list($type_cast);
    }

    protected function get_list_with_params(string $query, $params, $type_cast = false): AZList {
      //
      $this->set_query($query);
      $this->set_parameters($params);
      //
      return $this->get_list($type_cast);
    }

    protected function get_multi($type_cast = false): array {
      //
      $rtn_val = array();
      //
      if ($this->is_prepared()) {
        // prepared statement 사용의 경우

        $this->compile_prepared();
        //
        if (is_null($this->_statement)) {
          $this->_statement = $this->get_mysqli()->prepare($this->_compiled_query);
        }
        if (!$this->_statement) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        if ($this->_statement && $this->get_mysqli()->error && $this->get_mysqli()->errno != 0) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        //
        if (
            $this->get_parameters() && 
            $this->get_parameters()->size() > 0 &&
            $this->_prepared_parameter_types && 
            $this->_prepared_parameter_keys
          ) {
          $types = join('', $this->_prepared_parameter_types);
          $values = array();
          foreach ($this->_prepared_parameter_keys as $key) {
            array_push($values, $this->get_parameter($key));
          }
          $this->_statement->bind_param($types, ...$values);
        }
        //
        if ($this->_statement->execute()) {
          //
          while (
              ($result = $this->_statement->get_result()) ||
              ($this->_statement->more_results() && $this->_statement->next_result() && $result = $this->_statement->get_result())
            ) {
            $fields = array();
            while ($field = $result->fetch_field()) {
              array_push($fields, $field->name);
            }
            $list = AZList::create();
            while ($row = $result->fetch_array(MYSQLI_NUM)) {
              $rtn_data = AZData::create();
              //
              $idx = 0;
              foreach ($fields as $field) {
                $rtn_data->add($field, $row[$idx]);
                $idx++;
              }
              //
              $list->add($rtn_data);
            }
            array_push($rtn_val, $list);
            $result->free_result();
          }
          //
          $this->_statement->free_result();
          // $this->_statement->close();
        }
      }
      else {
        // prepared statement 가 아닌 일반 사용의 경우

        $this->compile();
        //
        $success = $this->get_mysqli()->multi_query($this->_compiled_query);
        if (!$success) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        if ($success && $this->get_mysqli()->error && $this->get_mysqli()->errno != 0) {
          throw new \Exception($this->get_mysqli()->error, $this->get_mysqli()->errno);
        }
        while (
            ($result = $this->get_mysqli()->store_result()) ||
            ($this->get_mysqli()->more_results() && $this->get_mysqli()->next_result() && $result = $this->get_mysqli()->store_result())
          ) {
          $fields = array();
          while ($field = $result->fetch_field()) {
            array_push($fields, $field->name);
          }
          $list = AZList::create();
          while ($row = $result->fetch_array(MYSQLI_NUM)) {
            $rtn_data = AZData::create();
            //
            $idx = 0;
            foreach ($fields as $field) {
              $rtn_data->add($field, $row[$idx]);
              $idx++;
            }
            //
            $list->add($rtn_data);
          }
          array_push($rtn_val, $list);
          unset($field);
          unset($type_cast);
          $result->free_result();
        }
      }

      //
      if (
          $this->is_stored_procedure() &&
          !is_null($this->get_return_parameters()) &&
          $this->get_return_parameters()->size() > 0
        ) {
        //
        $query = 'SELECT';
        //
        $idx = 0;
        $keys = $this->get_return_parameters()->keys();
        foreach ($keys as $key) {
          $query .= ($idx > 0 ? ', ' : ' ').$key.' AS '.str_replace('@', 'o_', $key);
          $idx++;
        }
        $result = $this->get_mysqli()->query($query);
        if ($result && $result->num_rows > 0) {
          //
          $fields = array();
          while ($field = $result->fetch_field()) {
            $fields[$field->name] = $field->type;
          }
          //
          $result->data_seek(0);
          //
          $data = $result->fetch_array(MYSQLI_ASSOC);
          $result->free_result();
          //
          foreach ($keys as $key) {
            $key_mod = str_replace('@', 'o_', $key);
            $value = $data[$key_mod];
            $this->value_caster($value, $fields[$key_mod]);
            $this->update_return_parameter($key, $value);
          }
          //
          $data = null;
        }
      }
      //
      return $rtn_val;
    }
    
    protected function get_multi_with_query(string $query, $type_cast = false): array {
      //
      $this->set_query($query);
      $this->clear_parameters();
      //
      return $this->get_multi($type_cast);
    }

    protected function get_multi_with_params(string $query, $params, $type_cast = false): array {
      //
      $this->set_query($query);
      $this->set_parameters($params);
      //
      return $this->get_multi($type_cast);
    }

    private function free_results(&$results) {
      while(isset($results->conn_id) && mysqli_more_results($results->conn_id) && mysqli_next_result($results->conn_id)) {
        if($l_result = mysqli_store_result($results->conn_id)) {
          mysqli_free_result($l_result);
        }
      }
    }
  }
}

namespace AZLib\AZSql {
  use \AZLib\AZData;
  use \AZLib\AZSql;

  class VALUETYPE {
    const VALUE = 1;
    const QUERY = 2;
  }

  class WHERETYPE {
    const GREATER_THAN = 'GREATER_THAN';
    const GT = 'GT';
    const GREATER_THAN_OR_EQUAL = 'GREATER_THAN_OR_EQUAL';
    const GTE = 'GTE';
    const LESS_THAN = 'LESS_THAN';
    const LT = 'LT';
    const LESS_THAN_OR_EQUAL = 'LESS_THAN_OR_EQUAL';
    const LTE = 'LTE';
    const EQUAL = 'EQUAL';
    const EQ = 'EQ';
    const NOT_EQUAL = 'NOT_EQUAL';
    const NE = 'NE';
    const BETWEEN = 'BETWEEN';
    const IN = 'IN';
    const NOT_IN = 'NOT_IN';
    const NIN = 'NIN';
    const LIKE = 'LIKE';
  }

  class CREATE_QUERY_TYPE {
    const INSERT = 1;
    const UPDATE = 2;
    const DELETE = 3;
    const SELECT = 4;
  }

  class SetData {
    public $_column;
    public $_value;
    public $_VALUETYPE;
  }

  class WhereData {
    public $_column;
    public $_value;
    public $_WHERETYPE;
    public $_VALUETYPE;
  }

  class BQuery {
    private $_table_name;
    private $_set_datas; // array
    private $_where_datas; // array
    private $_is_prepared = false;
    public function __construct(string $table_name) {
      $this->_table_name = $table_name;
    }

    public static function create(string $table_name) {
      return new BQuery($table_name);
    }
    
    public function __call($name, $args) {
      switch ($name) {
        case 'set':
          if (count($args) == 2) {
            array_push($args, null);
          }
          return call_user_func_array(array($this, 'set'), $args);
        case 'where':
          if (count($args) == 2) {
            array_push($args, null, null);
          }
          else if (count($args) == 3) {
            switch (gettype($args[2])) {
              case 'string':
                array_push($args, null);
                break;
              default:
                $args[3] = $args[2];
                $args[2] = null;
                break;
            }
          }
          return call_user_func_array(array($this, 'where'), $args);
      }
    }

    public function set_prepared($state) {
      $this->_is_prepared = $state;
      return $this;
    }

    public function is_prepared() {
      return $this->_is_prepared;
    }

    public function clear_set() {
      $this->_set_datas = null;
      return $this;
    }

    public function clear_where() {
      $this->_where_datas = null;
      return $this;
    }

    public function clear() {
      $this->clear_set();
      $this->clear_where();
      $this->set_prepared(false);
      return $this;
    }

    protected function set(string $column, $value, $VALUETYPE = null) {
      if (is_null($VALUETYPE)) $VALUETYPE = VALUETYPE::VALUE;
      //
      $set = new SetData();
      $set->_column = $column;
      $set->_value = $value;
      $set->_VALUETYPE = $VALUETYPE;
      //
      if (is_null($this->_set_datas)) $this->_set_datas = array();
      array_push($this->_set_datas, $set);
      //
      return $this;
    }

    protected function where(string $column, $value, $WHERETYPE = null, $VALUETYPE = null) {
      if (is_null($WHERETYPE)) $WHERETYPE = WHERETYPE::EQUAL;
      if (is_null($VALUETYPE)) $VALUETYPE = VALUETYPE::VALUE;
      //
      $set = new WhereData();
      $set->_column = $column;
      $set->_value = $value;
      $set->_WHERETYPE = $WHERETYPE;
      $set->_VALUETYPE = $VALUETYPE;
      //
      if (is_null($this->_where_datas)) $this->_where_datas = array();
      array_push($this->_where_datas, $set);
      //
      return $this;
    }

    public function compile($CREATE_QUERY_TYPE) {
      $query = null;
      $params = null;
      //
      switch ($CREATE_QUERY_TYPE) {
        case CREATE_QUERY_TYPE::INSERT:
          $col_str = '';
          $val_str = '';
          //
          $idx = 0;
          foreach ($this->_set_datas as $set) {
            $col_str .= PHP_EOL.($idx > 0 ? ' ,' : ' ').$set->_column;
            //
            if ($this->is_prepared() && $set->_VALUETYPE == VALUETYPE::VALUE) {
              // prepared statement 사용시
              if (is_null($params)) $params = AZData::create();
              //
              $key = "@__set_{$idx}_{$set->_column}";
              $params->add($key, $set->_value);
              $val_str .= PHP_EOL.($idx > 0 ? ' ,' : ' ').$key;
            }
            else {
              // prepared statement 미사용시
              $q_mark = "";
              if ($set->_VALUETYPE == VALUETYPE::VALUE) {
                $q_mark = $this->get_qmark($set->_value);
              }
              $val_str .= PHP_EOL.($idx > 0 ? ' ,' : ' ')."$q_mark{$set->_value}$q_mark";
            }
            $idx++;
          }
          $query = "INSERT INTO $this->_table_name ($col_str".PHP_EOL.")".PHP_EOL."VALUES ($val_str".PHP_EOL.")";
          return array('query' => $query, 'parameters' => $params);
        case CREATE_QUERY_TYPE::UPDATE:
          $query = "UPDATE $this->_table_name";
          //
          if (!is_null($this->_set_datas)) {
            $query .= PHP_EOL."SET";
            $idx = 0;
            foreach ($this->_set_datas as $set) {
              if ($this->is_prepared() && $set->_VALUETYPE == VALUETYPE::VALUE) {
                // prepared statement 사용시
                if (is_null($params)) $params = AZData::create();
                //
                $key = "@__set_{$idx}_{$set->_column}";
                $params->add($key, $set->_value);
                $query .= ($idx > 0 ? ',' : '').PHP_EOL." {$set->_column} = $key";
              }
              else {
                // prepared statement 미사용시
                $q_mark = "";
                if ($set->_VALUETYPE == VALUETYPE::VALUE) {
                  $q_mark = $this->get_qmark($set->_value);
                }
                $query .= ($idx > 0 ? ',' : '').PHP_EOL." {$set->_column} = $q_mark{$set->_value}$q_mark";
              }
              $idx++;
            }
          }
        case CREATE_QUERY_TYPE::DELETE:
          if (is_null($query)) $query = $query = "DELETE FROM $this->_table_name";
          //
          if (!is_null($this->_where_datas)) {
            $query .= PHP_EOL."WHERE";
            $idx = 0;
            foreach ($this->_where_datas as $where) {
              $val_cnt = 1;
              $condition = '=';
              switch ($where->_WHERETYPE) {
                case WHERETYPE::GREATER_THAN:
                case WHERETYPE::GT:
                  $condition = '>';
                  break;
                case WHERETYPE::GREATER_THAN_OR_EQUAL:
                case WHERETYPE::GTE:
                  $condition = '>=';
                  break;
                case WHERETYPE::LESS_THAN:
                case WHERETYPE::LT:
                  $condition = '<';
                  break;
                case WHERETYPE::LESS_THAN_OR_EQUAL:
                case WHERETYPE::LTE:
                  $condition = '<=';
                  break;
                case WHERETYPE::EQUAL:
                case WHERETYPE::EQ:
                  $condition = '=';
                  break;
                case WHERETYPE::NOT_EQUAL:
                case WHERETYPE::NE:
                  $condition = '<>';
                  break;
                case WHERETYPE::BETWEEN:
                  $condition = 'BETWEEN';
                  $val_cnt = 2;
                  break;
                case WHERETYPE::IN:
                  $condition = 'IN';
                  $val_cnt = 3;
                  break;
                case WHERETYPE::NOT_IN:
                case WHERETYPE::NIN:
                  $condition = 'NOT IN';
                  $val_cnt = 3;
                  break;
                case WHERETYPE::LIKE:
                  $condition = 'LIKE';
                  break;
              }
              $query .= PHP_EOL.($idx > 0 ? ' AND ' : ' ')."{$where->_column} $condition ";
              if ($this->is_prepared() && $where->_VALUETYPE == VALUETYPE::VALUE) {
                // prepared statement 사용시
                if (is_null($params)) $params = AZData::create();
                //
                switch ($val_cnt) {
                  case 1:
                    $key = "@__where_{$idx}_{$where->_column}";
                    $params->add($key, $where->_value);
                    $query .= $key;
                    break;
                  case 2:
                    $keys = array(
                      "@__where_{$idx}_{$where->_column}_btw_1",
                      "@__where_{$idx}_{$where->_column}_btw_2"
                    );
                    $params
                      ->add($keys[0], $where->_value[0])
                      ->add($keys[1], $where->_value[1]);
                    $query .= "$keys[0] AND $keys[1]";
                    break;
                  case 3:
                    $query .= '(';
                    $jdx = 0;
                    foreach ($where->_value as $val) {
                      //
                      $key = "@__where_{$idx}_{$where->_column}_in_$jdx";
                      $params->add($key, $val);
                      //
                      $query .= ($jdx > 0 ? ',' : '').$key;
                      $jdx++;
                    }
                    $query .= ')';
                    break;
                }
              }
              else {
                // prepared statement 미사용시
                $q_mark = '';
                if ($where->_VALUETYPE == VALUETYPE::VALUE) {
                  $q_mark = $this->get_qmark($where->_value);
                }
                switch ($val_cnt) {
                  case 1:
                    $query .= "$q_mark{$where->_value}$q_mark";
                    break;
                  case 2:
                    $jdx = 0;
                    foreach ($where->_value as $val) {
                      //
                      $q_mark = $this->get_qmark($val);
                      //
                      $query .= ($jdx > 0 ? ' AND ' : '')."$q_mark{$val}$q_mark";
                      $jdx++;
                    }
                    // $query .= "$q_mark$where->_value[0]$q_mark AND $q_mark$where->_value[1]$q_mark";
                    break;
                  case 3:
                    $query .= '(';
                    $jdx = 0;
                    foreach ($where->_value as $val) {
                      //
                      $q_mark = $this->get_qmark($val);
                      //
                      $query .= ($jdx > 0 ? ',' : '')."$q_mark{$val}$q_mark";
                      $jdx++;
                    }
                    $query .= ')';
                    break;
                }
              }
              $idx++;
            }
          }
          return array('query' => $query, 'parameters' => $params);
      }
    }

    private function get_qmark($val) {
      $rtn_val = '';
      switch (gettype($val)) {
        case 'integer':
        case 'float':
        case 'double':
          $rtn_val = '';
          break;
        default:
          $rtn_val = "'";
          break;
      }
      return $rtn_val;
    }
  }

  class Basic {
    private $bquery;
    private $_db;
    //
    public function __construct(string $table_name, &$db = null) {
      //
      if (!is_null($db)) {
        $this->_db = $db;
      }
      //
      $this->bquery = new BQuery($table_name);
    }

    public static function create(string $table_name, &$db = null) {
      return new Basic($table_name, $db);
    }

    public function __call($name, $args) {
      switch ($name) {
        case 'set':
          if (count($args) == 2) {
            array_push($args, null);
          }
          return call_user_func_array(array($this, 'set'), $args);
        case 'where':
          if (count($args) == 2) {
            array_push($args, null, null);
          }
          else if (count($args) == 3) {
            switch (gettype($args[2])) {
              case 'string':
                array_push($args, null);
                break;
              default:
                $args[3] = $args[2];
                $args[2] = null;
                break;
            }
          }
          return call_user_func_array(array($this, 'where'), $args);
        case 'do_insert':
          if (count($args) == 0) {
            array_push($args, false);
          }
          return call_user_func_array(array($this, 'do_insert'), $args);
      }
    }

    public function set_prepared($state) {
      $this->bquery->set_prepared($state);
      return $this;
    }

    public function is_prepared() {
      return $this->bquery->is_prepared();
    }

    public function clear_set() {
      $this->bquery->clear_set();
      return $this;
    }

    public function clear_where() {
      $this->bquery->clear_where();
      return $this;
    }

    public function clear() {
      $this->bquery->clear();
      return $this;
    }

    protected function set(string $column, $value, $VALUETYPE = null) {
      $this->bquery->set($column, $value, $VALUETYPE);
      return $this;
    }

    protected function where(string $column, $value, $WHERETYPE = null, $VALUETYPE = null) {
      $this->bquery->where($column, $value, $VALUETYPE);
      return $this;
    }

    public function do_insert($identity = false) {
      // $time = microtime();
      $compiled = $this->bquery->compile(CREATE_QUERY_TYPE::INSERT);
      //
      $sql = AZSql::create($this->_db)
        ->set_prepared($this->is_prepared())
        ->set_query($compiled['query']);
      if ($this->is_prepared()) {
        $sql->set_parameters($compiled['parameters']);
      }
      // $this->_db['elapsed'] = microtime() - $time;
      return $sql->execute($identity);
    }

    public function do_update() {
      $compiled = $this->bquery->compile(CREATE_QUERY_TYPE::UPDATE);
      //
      $sql = AZSql::create($this->_db)
        ->set_prepared($this->is_prepared())
        ->set_query($compiled['query']);
      if ($this->is_prepared()) {
        $sql->set_parameters($compiled['parameters']);
      }
      return $sql->execute();
    }

    public function do_delete() {
      $compiled = $this->bquery->compile(CREATE_QUERY_TYPE::DELETE);
      //
      $sql = AZSql::create($this->_db)
        ->set_prepared($this->is_prepared())
        ->set_query($compiled['query']);
      if ($this->is_prepared()) {
        $sql->set_parameters($compiled['parameters']);
      }
      return $sql->execute();
    }
  }
}
/*
  // test code
  $this->load->helper('azlib');
  echo "<br /><br /><br /><br /><br /><br />";
  echo "class:".get_class($this->db)."<br />";
  echo "class:".get_class($this->db->conn_id)."<br />";
  // execute
  $e1 = AZSql::create($this->db)->execute('INSERT INTO _temp (id, name) VALUES (@id, @name)', AZData::create()->add('@id', 'id1')->add('@name', 'name1'));
  echo "e1:".$e1."<br />";
  $e1 = AZSql::create($this->db)->execute('INSERT INTO _temp (id, name) VALUES (@id, @name)', AZData::create()->add('@id', 'id1')->add('@name', 'name1'), true);
  echo "e1:".$e1."<br />";
  $e1 = AZSql::create($this->db)->set_prepared(true)->execute('INSERT INTO _temp (id, name) VALUES (@id, @name)', AZData::create()->add('@id', 'id1')->add('@name', 'name1'), true);
  echo "e1:".$e1."<br />";
  //
  $msc = microtime(true);
  $fetch = $this->db->conn_id->query('SELECT * FROM qz ORDER BY qz_num DESC LIMIT 1, 1')->fetch_assoc();
  echo "msc:".(microtime(true) - $msc)."<br />";
  echo "n1:".json_encode($fetch)."<br />";
  $sql = AZSql::create($this->db);
  $sql
    ->set_query('SELECT * FROM qz ORDER BY qz_num DESC LIMIT @st, @st')
    ->set_parameters(AZData::create()->add('@st', 1));
  //$list_n1 = $sql->set_prepared(false)->get_data('SELECT * FROM qz ORDER BY qz_num DESC LIMIT @st, @st', AZData::create()->add('@st', 1));
  echo "n2:".$sql->get_data()->to_json_string()."<br />";
  echo "n2:".$sql->set_query('SELECT * FROM qz ORDER BY qz_num LIMIT @st, @st')->get_data()->to_json_string()."<br />";
  $list_n1 = $sql->set_prepared(false)->get_data('SELECT * FROM qz ORDER BY qz_num DESC LIMIT @st, @st', AZData::create()->add('@st', 1), true);
  echo "n3.cast:".$list_n1->to_json_string()."<br />";
  $list_n1 = $sql->set_prepared(true)->get_data('SELECT * FROM qz ORDER BY qz_num DESC LIMIT @st, @st', AZData::create()->add('@st', 1), true);
  echo "n3.cast.prepared:".$list_n1->to_json_string()."<br />";
  $list_n1 = $sql->set_prepared(true)->get_data('SELECT * FROM qz ORDER BY qz_num DESC LIMIT @st, @st', AZData::create()->add('@st', 1), true);
  echo "n3.cast.prepared:".$list_n1->to_json_string()."<br />";
  //
  $sp1 = $sql
    ->clear()
    ->set_prepared(true)
    ->set_stored_procedure(true)
    ->set_query('CALL pro_test_output(@src, @add, @out)')
    ->add_parameter('@src', 10)
    ->add_parameter('@add', 55)
    ->add_return_parameter('@out', 0)
    ->get_data();
    echo "sp1.1:".$sp1->to_json_string()."<br />";
    echo "sp1.2:".$sql->get_return_parameters()->to_json_string()."<br />";
    echo "sp1.3:".$sql->get_return_parameter('@out')."<br />";
  //
  $pl1 = $sql
    ->clear()
    ->set_prepared(true)
    ->set_query('SELECT * FROM qz ORDER BY qz_num LIMIT @st, @ed')
    ->set_parameters(AZData::create()->add('@st', 1)->add('@ed', 5))
    ->get_list();
    echo "pl1.1:".$pl1->to_json_string()."<br />";
  $l1 = $sql
    ->clear()
    ->set_query('SELECT * FROM qz ORDER BY qz_num LIMIT @st, @ed')
    ->set_parameters(AZData::create()->add('@st', 1)->add('@ed', 5))
    ->get_list();
    echo "l1.1:".$l1->to_json_string()."<br />";
  $pl2 = $sql
    ->clear()
    ->set_prepared(true)
    ->set_stored_procedure(true)
    ->set_query('CALL pro_test_output(@src, @add, @out)')
    ->set_parameters(AZData::create()->add('@src', 1)->add('@add', 2))
    ->add_return_parameter('@out', 0)
    ->get_multi();
    echo "pl2.1:".json_encode($pl2)."<br />";
    echo "pl2.2:".$sql->get_return_parameter('@out')."<br />";
  //
  $t1 = $sql->get('SELECT vod_key FROM qz ORDER BY qz_num DESC LIMIT @st, @st', AZData::create()->add('@st', 1), true);
  echo "t1.n:".$t1."<br />";
  $t1 = $sql->set_prepared(true)->get('SELECT vod_key FROM qz ORDER BY qz_num DESC LIMIT @st, @st', AZData::create()->add('@st', 1), true);
  echo "t1.p:".$t1."<br />";
  die;
*/