<?php
/**
 * AZData
 */
class AZData implements Iterator, \JsonSerializable {
  // protected $CI;
  private $_key = 0;
  private $_keys = null;
  private $_data = null;
  public function __construct($json = null) {
    // $this->CI =& get_instance();
    //
    if (!is_null($json)) {
      // echo "__const - type:".gettype($json)."\n";
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
    
  // iterator 구현용
  public function current() {
    // echo "__current - _key:".$this->_key.":".$this->get($this->_key)."<br />";
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

  public function keys() {
    return array_keys($this->_data);
  }

  public function get_key($idx): string {
    return array_keys($this->_data)[$idx];
  }

  protected function get_by_index($idx) {
    $cnt = count($this->_data);
    // echo "__get_by_index - idx:".$idx." / cnt:$cnt / key:".$this->_keys[$idx]."<br />";
    if ($idx < 0 || $idx >= $cnt) {
      return null;
    }
    return $this->_data[$this->_keys[$idx]];
  }

  protected function get_by_key(string $key) {
    // echo "__get_by_index - key:".$key."<br />";
    if (!$this->has_key($key)) {
      return null;
    }
    return $this->_data[$key];
  }

  public function size() {
    return count($this->_keys);
  }

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
        // echo "convert - name:".$name." / value:".$this->get($name)." / isStatic:".$property->isStatic()."\n";
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
          // echo "\nto_json - key:{$keys[$i]} - {$rtn_val[$keys[$i]]->to_json()}\n";
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

class AZList implements Iterator, \JsonSerializable {
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
  //
  public function __construct(&$db = null) {
    //
    if (!is_null($db)) {
      $this->_db = $db;
    }
    //
    $this->_parameters = AZData::create();
  }

  public static function create(&$db = null) {
    return new AZSql($db);
  }

  protected function is_mysqli() {
    return get_class($this->_db->conn_id) == 'mysqli';
  }
  
  protected function get_mysqli() {
    return $this->is_mysqli() ? $this->_db->conn_id : null;
  }

  private function param_replacer(string $query, string $key, $value) {
    $query = preg_replace("/{$key}$/", $this->_db->escape($value), $query);
    $query = preg_replace("/{$key}\\r\\n/", $this->_db->escape($value)."\\r\\n", $query);
    $query = preg_replace("/{$key}\\n/", $this->_db->escape($value)."\\n", $query);
    $query = preg_replace("/{$key}\s/", $this->_db->escape($value)." ", $query);
    $query = preg_replace("/{$key}\t/", $this->_db->escape($value)."\\t", $query);
    $query = preg_replace("/{$key},/", $this->_db->escape($value).",", $query);
    $query = preg_replace("/{$key}\)/", $this->_db->escape($value).")", $query);
    return $query;
  }

  private function prepared_query_param_replacer(string &$query, $data) {
    // 반환자료 생성
    $rtn_val = array('types' => array(), 'values' => array());
    //
    foreach ($data as $key => $value) {
      $replace_cnt = 0;
      $query = preg_replace_callback("/{$key}$/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'; }, $query);
      $query = preg_replace_callback("/{$key}\\r\\n/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'."\\r\\n"; }, $query);
      $query = preg_replace_callback("/{$key}\\n/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'."\\n"; }, $query);
      $query = preg_replace_callback("/{$key}\s/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'." "; }, $query);
      $query = preg_replace_callback("/{$key}\t/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'."\\t"; }, $query);
      $query = preg_replace_callback("/{$key},/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'.","; }, $query);
      $query = preg_replace_callback("/{$key}\)/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'.")"; }, $query);
      // echo "replace_cnt:".$replace_cnt."<br />";
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

  private function prepared_query_replacer(string &$query, $data) {
    // 반환자료 생성
    $rtn_val = array('types' => array(), 'keys' => array());
    //
    foreach ($data as $key => $value) {
      $replace_cnt = 0;
      $query = preg_replace_callback("/{$key}$/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'; }, $query);
      $query = preg_replace_callback("/{$key}\\r\\n/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'."\\r\\n"; }, $query);
      $query = preg_replace_callback("/{$key}\\n/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'."\\n"; }, $query);
      $query = preg_replace_callback("/{$key}\s/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'." "; }, $query);
      $query = preg_replace_callback("/{$key}\t/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'."\\t"; }, $query);
      $query = preg_replace_callback("/{$key},/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'.","; }, $query);
      $query = preg_replace_callback("/{$key}\)/", function ($match) use (&$replace_cnt) { $replace_cnt++; return '?'.")"; }, $query);
      // echo "replace_cnt:".$replace_cnt."<br />";
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

  private function value_caster(&$value, $type) {
    // echo "caster - value:".$value." / ".gettype($value)."<br />";
    // echo "caster - type:".$type." / ".gettype($type)."<br />";
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
    /*
    switch ($type) {
      case 'tinyint':
      case 'smallint':
      case 'mediumint':
      case 'integer':
      case 'bigint':
        $value = intval($value);
        break;
      case 'float':
        $value = floatval($value);
        break;
      case 'decimal':
      case 'double':
        $value = doubleval($value);
        break;
    }
    */
  }

  private function data_value_caster(array &$data, $metadata) {
    // $keys = array_keys($data);
    // echo "caster - data:".json_encode($data)."<br />";
    // echo "caster - metadata:".json_encode($metadata)."<br />";
    foreach ($data as $key => &$value) {
      foreach ($metadata as $meta) {
        if ($meta->name === $key) {
          $this->value_caster($value, $meta->type);
          break;
        }
      }
    }
  }

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
    }
  }

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

  public function get_query(): string {
    return $this->_query;
  }

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

  public function get_compiled_query(): string {
    if ($this->is_prepared()) {
      $this->compile_prepared();
    }
    else {
      $this->compile();
    }
    return $this->_compiled_query;
  }

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

  protected function compile_prepared() {
    //
    if (
        !_compiled_query ||
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
    }
    //
    return $this;
  }

  public function set_parameters(AZData &$params) {
    $this->_parameters = $params;
    /*
    if (gettype($params) === 'object' && $params instanceof AZData) {
      $this->_parameters = $params->to_json();
    }
    */
    return $this;
  }

  public function get_parameters() {
    return $this->_parameters;
  }

  public function get_parameter(string $key) {
    return $this->_parameters->get($key);
  }

  public function add_parameter(string $key, $value) {
    if (!$this->is_prepared()) $this->_compiled_query = null;
    $this->_parameters->add($key, $value);
    return $this;
  }

  public function remove_parameter(string $key) {
    if (!$this->is_prepared()) $this->_compiled_query = null;
    $this->_parameters->remove($key);
    return $this;
  }

  public function clear_parameters() {
    if (!$this->is_prepared()) $this->_compiled_query = null;
    $this->_parameters->clear();
    return $this;
  }

  public function set_return_parameters(AZData &$params) {
    $this->_return_parameters = $params;
    return $this;
  }

  public function get_return_parameters() {
    return $this->_return_parameters;
  }

  public function get_return_parameter(string $key) {
    return $this->_return_parameters->get($key);
  }

  public function add_return_parameter(string $key, $value) {
    if (!$this->is_prepared()) $this->_compiled_query = null;
    if (is_null($this->_return_parameters)) {
      $this->_return_parameters = AZData::create();
    }
    $this->_return_parameters->add($key, $value);
    return $this;
  }

  public function update_return_parameter(string $key, $value) {
    if (!is_null($this->_return_parameters)) {
      $this->_return_parameters->set($key, $value);
    }
    return $this;
  }

  public function remove_return_parameter(string $key) {
    if (!$this->is_prepared()) $this->_compiled_query = null;
    if (is_null($this->_return_parameters)) {
      $this->_return_parameters->remove($key);
    }
    return $this;
  }

  public function clear_return_parameters() {
    if (!$this->is_prepared()) $this->_compiled_query = null;
    $this->_return_parameters = null;
    return $this;
  }

  public function set_prepared($state) {
    //
    $this->_is_prepared = $state;
    //
    $this->_compiled_query = null;
    //
    $this->_prepared_parameter_types = null;
    $this->_prepared_parameter_keys = null;
    //
    return $this;
  }

  public function is_prepared() {
    return $this->_is_prepared;
  }

  public function set_stored_procedure($state) {
    $this->_is_stored_procedure = $state;
    return $this;
  }

  public function is_stored_procedure() {
    return $this->_is_stored_procedure;
  }

  public function clear() {
    // private $_query; // string
    // private $_compiled_query; // 실제 요청시 사용될 쿼리문
    // private $_parameters; // AZData
    // private $_prepared_parameter_types; // array, prepared statement 사용시 사용될 parameters 의 type 목록
    // private $_prepared_parameter_keys; // array, prepared statement 사용시 사용될 parameters 의 key 목록
    // private $_return_parameters; // AZData, out 반환값 설정 자료

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

  public function begin_tran(callable $on_commit, callable $on_rollback) {
    $this->get_mysqli()->autocommit(false);
    //
    $this->_is_transaction = true;
    //
    if ($on_commit) $this->_action_tran_on_commit = $on_commit;
    if ($on_rollback) $this->_action_tran_on_rollback = $on_rollback;
  }

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

  protected function execute(bool $identity = false): int {
    //
    $rtn_val = 0;
    //
    if ($this->is_prepared()) {
      // prepared statement 사용의 경우

      $this->compile_prepared();
      // echo "compile_prepared - query:".$this->get_query()."<br />";
      // echo "compile_prepared - _compiled_query:".$this->_compiled_query."<br />";
      // echo "compile_prepared - _prepared_parameter_types:".json_encode($this->_prepared_parameter_types)."<br />";
      // echo "compile_prepared - _prepared_parameter_keys:".json_encode($this->_prepared_parameter_keys)."<br />";
      //
      $stmt = $this->get_mysqli()->prepare($this->_compiled_query);
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
        $stmt->bind_param($types, ...$values);
      }
      //
      if ($stmt->execute()) {
        // echo "result - stmt.more_result:".$stmt->more_results()."<br />";
        if (gettype($result) == 'object') {
          $result->free_result();
        }
        //
        $rtn_val = $identity ? $stmt->insert_id() : $stmt->affected_rows();
        //
        // $idx = 0;
        while ($stmt->more_results() && $stmt->next_result()) {
          if ($result = $stmt->get_result()) {
            $result->free_result();
          }
        }
        //
        $stmt->free_result();
        $stmt->close();
      }
    }
    else {
      // prepared statement 가 아닌 일반 사용의 경우

      $this->compile();
      echo "compile - query:".$this->get_query()."<br />";
      echo "compile - _compiled_query:".$this->_compiled_query."<br />";
      //
      $result = $this->_db->query($this->_compiled_query);
      echo "compile - result:".$result."<br />";
      if ($result && $this->_db->error() && $this->_db->error()->code != 0) {
        echo "compile - error:".json_encode($this->_db->error())."<br />";
      }
      $rtn_val = $identity ? $this->_db->insert_id() : $this->_db->affected_rows();
      //
      if (gettype($result) == 'object') {
        $this->free_results($result);
        $result->free_result();
      }
    }
    //
    return $rtn_val;
  }

  protected function execute_with_query(string $query, bool $identity = false): int {
    //
    $this->set_query($query);
    $this->clear_parameters();
    //
    return $this->execute($identity);
    /*
    $rtn_val = 0;
    // $this->_db->simple_query($query);
    $q_result = $this->_db->query($query);
    $rtn_val = $identity ? $this->_db->insert_id() : $this->_db->affected_rows();
    //
    $q_result->free_result();
    $this->free_results($q_result);
    return $rtn_val;
    */
  }

  protected function execute_with_params(string $query, $params, bool $identity = false): int {
    //
    $this->set_query($query);
    $this->set_parameters($params);
    //
    return $this->execute($identity);
    /*
    if (gettype($params) === 'object' && $params instanceof AZData) {
      $params = $params->to_json();
    }
    //
    foreach ($params as $key => $value) {
      $query = $this->param_replacer($query, $key, $value);
    }
    //
    $rtn_val = -1;
    // $q_result = $this->_db->simple_query($query);
    $q_result = $this->_db->query($query);
    if ($q_result) {
      $rtn_val = $identity ? $this->_db->insert_id() : $this->_db->affected_rows();
    }
    //
    $q_result->free_result();
    $this->free_results($q_result);
    return $rtn_val;
    */
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
    /*
    $rtn_val = null;
    $q_result = $this->_db->query($query);
    $data = $q_result->row_array();
    if (count($data) > 0) {
      $rtn_val = array_shift($data);
      //
      if ($type_cast) {
        $field = $q_result->field_data();
        // echo "get.field:".json_encode($field)."\n";
        $this->value_caster($rtn_val, $field[0]->type);
        unset($field);
      }
    }
    //
    unset($data);
    unset($type_cast);
    //
    $q_result->free_result();
    $this->free_results($q_result);
    //
    return $rtn_val;
    */
  }

  protected function get_with_params(string $query, $params, $type_cast = false) {
    //
    $this->set_query($query);
    $this->set_parameters($params);
    //
    return $this->get($type_cast);
    /*
    if (gettype($params) === 'object' && $params instanceof AZData) {
      $params = $params->to_json();
    }
    //
    foreach ($params as $key => $value) {
      $query = $this->param_replacer($query, $key, $value);
    }
    //
    $rtn_val = null;
    $q_result = $this->_db->query($query);
    $data = $q_result->row_array();
    if (count($data) > 0) {
      $rtn_val = array_shift($data);
      //
      if ($type_cast) {
        $field = $q_result->field_data();
        // echo "get.field:".json_encode($field)."\n";
        $this->value_caster($rtn_val, $field[0]->type);
        unset($field);
      }
    }
    //
    unset($data);
    unset($params);
    unset($type_cast);
    //
    $q_result->free_result();
    $this->free_results($q_result);
    //
    return $rtn_val;
    */
  }

  protected function get_data($type_cast = false): AZData {
    //
    $rtn_val = AZData::create();
    //
    if ($this->is_prepared()) {
      // prepared statement 사용의 경우

      $this->compile_prepared();
      // echo "compile_prepared - query:".$this->get_query()."<br />";
      // echo "compile_prepared - _compiled_query:".$this->_compiled_query."<br />";
      // echo "compile_prepared - _prepared_parameter_types:".json_encode($this->_prepared_parameter_types)."<br />";
      // echo "compile_prepared - _prepared_parameter_keys:".json_encode($this->_prepared_parameter_keys)."<br />";
      //
      $stmt = $this->get_mysqli()->prepare($this->_compiled_query);
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
        $stmt->bind_param($types, ...$values);
      }
      //
      if ($stmt->execute()) {
        // echo "result - stmt.more_result:".$stmt->more_results()."<br />";
        $result = $stmt->get_result();
        // echo "result:".json_encode($result->fetch_array(MYSQLI_NUM))."<br />";
        $fields = array();
        while ($field = $result->fetch_field()) {
          array_push($fields, $field->name);
        }
        while ($row = $result->fetch_array(MYSQLI_NUM)) {
          //
          // echo "row:".json_encode($row)."<br />";
          // echo "fields:".json_encode($fields)."<br />";
          $idx = 0;
          foreach ($fields as $field) {
            $rtn_val->add($field, $row[$idx]);
            $idx++;
          }
        }
        $result->free_result();
        //
        // $idx = 0;
        while ($stmt->more_results() && $stmt->next_result()) {
          // echo "stmt more_results<br />";
          if ($result = $stmt->get_result()) {
            // echo "stmt more_results - result:".json_encode($result->fetch_array(MYSQLI_NUM))."<br />";
            $result->free_result();
          }
          /*
          if ($idx > 10) {
            echo "stmt BREAK!!<br />";
            break;
          }
          $idx++;
          */
        }
        //
        $stmt->free_result();
        $stmt->close();
      }
    }
    else {
      // prepared statement 가 아닌 일반 사용의 경우

      $this->compile();
      // echo "compile - query:".$this->get_query()."<br />";
      // echo "compile - _compiled_query:".$this->_compiled_query."<br />";
      //
      $result = $this->_db->query($this->_compiled_query);
      $data = $result->row_array();
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
      // echo "T:".mysqli_more_results($q_result->conn_id)."<br />";
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
      // echo "return - query: ".$query."<br />";
      $result = $this->get_mysqli()->query($query);
      if ($result && $result->num_rows > 0) {
        //
        $fields = array();
        while ($field = $result->fetch_field()) {
          $fields[$field->name] = $field->type;
        }
        // echo "return - fields: ".json_encode($fields)."<br />";
        //
        $result->data_seek(0);
        //
        $data = $result->fetch_array(MYSQLI_ASSOC);
        // echo "return - data: ".json_encode($data)."<br />";
        $result->free_result();
        //
        foreach ($keys as $key) {
          $key_mod = str_replace('@', 'o_', $key);
          $value = $data[$key_mod];
          // echo "return - key_mod: ".$key_mod." / value:".$value." / type:".$fields[$key_mod]."<br />";
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
    /*
    // echo "get_data.query:".$query."\n";
    $q_result = $this->_db->query($query);
    //echo "error:".json_encode($this->_db->error())."<br />";
    $data = $q_result->row_array();
    // echo "\nget_data.data.PRE:".json_encode($data)."\n";
    if ($type_cast) {
      $field = $q_result->field_data();
      // echo "get_data.field:".json_encode($field)."\n";
      $this->data_value_caster($data, $field);
      unset($field);
    }
    //
    unset($type_cast);
    //
    $this->free_results($q_result);
    $q_result->free_result();
    // echo "get_data.data.POST:".json_encode($data)."\n";
    // return AZData::parse($this->_db->query($query)->row_array());
    return AZData::parse($data);
    */
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
    /*
    $msc = microtime(true);
    if (gettype($params) === 'object' && $params instanceof AZData) {
      $params = $params->to_json();
    }
    //
    if ($this->is_prepared()) {
      // prepared statement 사용하는 경우
      $binds = $this->prepared_query_param_replacer($query, $params);
      // echo "query:".$query."<br />";
      // echo "types:".join('', $binds['types'])."<br />";
      // echo "values:".json_encode($binds['values'])."<br />";
      $stmt = $this->get_mysqli()->prepare($query);
      if ($binds && $binds['values'] && count($binds['values']) > 0) {
        $stmt->bind_param(join('', $binds['types']), ...$binds['values']);
      }
      if (!$stmt->execute()) {
        return null;
      }
      $q_result = $stmt->get_result();
      // echo "q_result:".json_encode($q_result)."<br />";
      $fields = array();
      while ($field = $q_result->fetch_field()) {
        array_push($fields, $field->name);
      }
      $rtn_val = AZData::create();
      while ($row = $q_result->fetch_array(MYSQLI_NUM)) {
        //
        // echo "row:".json_encode($row)."<br />";
        // echo "fields:".json_encode($fields)."<br />";
        $idx = 0;
        foreach ($fields as $field) {
          $rtn_val->add($field, $row[$idx]);
          $idx++;
        }
      }
      //
      unset($binds);
      unset($fields);
      $q_result->free_result();
      $stmt->free_result();
      $stmt->close();
      //
      $msc = microtime(true) - $msc;
      echo "msc:".$msc."<br />";
      return $rtn_val;
    }
    else {
      // non prepared statement 인 경우
      //
      foreach ($params as $key => $value) {
        $query = $this->param_replacer($query, $key, $value);
      }
      //
      $q_result = $this->_db->query($query);
      $data = $q_result->row_array();
      if ($type_cast) {
        $field = $q_result->field_data();
        $this->data_value_caster($data, $field);
        unset($field);
      }
      //
      unset($params);
      unset($type_cast);
      // echo "T:".mysqli_more_results($q_result->conn_id)."<br />";
      $this->free_results($q_result);
      $q_result->free_result();
      //
      // return AZData::parse($this->_db->query($query)->row_array());
      $msc = microtime(true) - $msc;
      echo "msc:".$msc."<br />";
      return AZData::parse($data);
    }
    */
    /*
    // echo "with_params - params.type:".gettype($params)."\n";
    // echo "with_params - params is instanceof:".($params instanceof AZData)."\n";
    if (gettype($params) === 'object' && $params instanceof AZData) {
      $params = $params->to_json();
    }
    //
    foreach ($params as $key => $value) {
      $query = $this->param_replacer($query, $key, $value);
    }
    //
    $q_result = $this->_db->query($query);
    $data = $q_result->row_array();
    if ($type_cast) {
      $field = $q_result->field_data();
      $this->data_value_caster($data, $field);
      unset($field);
    }
    //
    unset($params);
    unset($type_cast);
    // echo "T:".mysqli_more_results($q_result->conn_id)."<br />";
    $this->free_results($q_result);
    $q_result->free_result();
    //
    // return AZData::parse($this->_db->query($query)->row_array());
    return AZData::parse($data);
    */
  }

  /**
   * 지정된 쿼리 문자열에 대한 다행 결과를 AZList 객체로 반환
   */
  protected function get_list($type_cast = false): AZList {
    //
    $rtn_val = AZList::create();
    //
    if ($this->is_prepared()) {
      // prepared statement 사용의 경우

      $this->compile_prepared();
      // echo "compile_prepared - query:".$this->get_query()."<br />";
      // echo "compile_prepared - _compiled_query:".$this->_compiled_query."<br />";
      // echo "compile_prepared - _prepared_parameter_types:".json_encode($this->_prepared_parameter_types)."<br />";
      // echo "compile_prepared - _prepared_parameter_keys:".json_encode($this->_prepared_parameter_keys)."<br />";
      //
      $stmt = $this->get_mysqli()->prepare($this->_compiled_query);
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
        $stmt->bind_param($types, ...$values);
      }
      //
      if ($stmt->execute()) {
        // echo "result - stmt.more_result:".$stmt->more_results()."<br />";
        $result = $stmt->get_result();
        // echo "result:".json_encode($result->fetch_array(MYSQLI_NUM))."<br />";
        $fields = array();
        while ($field = $result->fetch_field()) {
          array_push($fields, $field->name);
        }
        while ($row = $result->fetch_array(MYSQLI_NUM)) {
          $rtn_data = AZData::create();
          //
          // echo "row:".json_encode($row)."<br />";
          // echo "fields:".json_encode($fields)."<br />";
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
        // $idx = 0;
        while ($stmt->more_results() && $stmt->next_result()) {
          // echo "stmt more_results<br />";
          if ($result = $stmt->get_result()) {
            // echo "stmt more_results - result:".json_encode($result->fetch_array(MYSQLI_NUM))."<br />";
            $result->free_result();
          }
          /*
          if ($idx > 10) {
            echo "stmt BREAK!!<br />";
            break;
          }
          $idx++;
          */
        }
        //
        $stmt->free_result();
        $stmt->close();
      }
    }
    else {
      // prepared statement 가 아닌 일반 사용의 경우

      $this->compile();
      // echo "compile - query:".$this->get_query()."<br />";
      // echo "compile - _compiled_query:".$this->_compiled_query."<br />";
      //
      $result = $this->_db->query($this->_compiled_query);
      $list = $result->result_array();
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
      // echo "T:".mysqli_more_results($result->conn_id)."<br />";
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
      // echo "return - query: ".$query."<br />";
      $result = $this->get_mysqli()->query($query);
      if ($result && $result->num_rows > 0) {
        //
        $fields = array();
        while ($field = $result->fetch_field()) {
          $fields[$field->name] = $field->type;
        }
        // echo "return - fields: ".json_encode($fields)."<br />";
        //
        $result->data_seek(0);
        //
        $data = $result->fetch_array(MYSQLI_ASSOC);
        // echo "return - data: ".json_encode($data)."<br />";
        $result->free_result();
        //
        foreach ($keys as $key) {
          $key_mod = str_replace('@', 'o_', $key);
          $value = $data[$key_mod];
          // echo "return - key_mod: ".$key_mod." / value:".$value." / type:".$fields[$key_mod]."<br />";
          $this->value_caster($value, $fields[$key_mod]);
          $this->update_return_parameter($key, $value);
        }
        //
        $data = null;
      }
    }
    //
    return $rtn_val;
    /*
    //
    $rtn_val = new AZList();
    //
    $q_result = $this->_db->query($query);
    //
    $list = $q_result->result_array();
    $field = null;
    if ($type_cast) {
      $field = $q_result->field_data();
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
    //
    $this->free_results($q_result);
    $q_result->free_result();
    return $rtn_val;
    */
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
    /*
    //
    $rtn_val = new AZList();
    //
    if (gettype($params) === 'object' && $params instanceof AZData) {
      $params = $params->to_json();
    }
    //
    foreach ($params as $key => $value) {
      $query = $this->param_replacer($query, $key, $value);
    }
    //
    // $q_result = null;
    // $field = null;
    //
    /*
    if ($this->is_raw()) {
      $q_result = $this->_db->get_mysqli()->query($query);
      $field = array();
      while ($field_data = $q_result->fetch_field()) {
        array_push($field, array('name' => $field_data->name, 'type' => $field_data->type));
      }
      //
      $list = $q_result->result_array();
      $field = null;
      if ($type_cast) {
        $field = $q_result->field_data();
      }
    }
    else {
      $q_result = $this->_db->query($query);
      while ($field_data = $q_result->fetch_field()) {
        echo "field:".json_encode(array('name' => $field_data->name, 'type' => $field_data->type, 'length' => $field_data->length))."<br />";
        echo "field:".json_encode($field_data)."<br />";
      }
      foreach ($q_result as $res) {
        echo "res.#:".json_encode($res)."<br />";
      }
      //
      $list = $q_result->result_array();
      $field = null;
      if ($type_cast) {
        $field = $q_result->field_data();
      }
    }
    *
    
    $q_result = $this->_db->query($query);
    $list = $q_result->result_array();
    $field = null;
    if ($type_cast) {
      $field = $q_result->field_data();
      echo "field:".json_encode($field)."<br />";
    }
    foreach ($list as $data) {
      if ($type_cast) {
        $this->data_value_caster($data, $field);
      }
      $rtn_val->add(AZData::parse($data));
    }
    //
    unset($field);
    unset($params);
    unset($type_cast);
    //
    $this->free_results($q_result);
    $q_result->free_result();
    //
    return $rtn_val;
    */
  }

  protected function get_multi($type_cast = false): array {
    //
    $rtn_val = array();
    //
    if ($this->is_prepared()) {
      // prepared statement 사용의 경우

      $this->compile_prepared();
      // echo "compile_prepared - query:".$this->get_query()."<br />";
      // echo "compile_prepared - _compiled_query:".$this->_compiled_query."<br />";
      // echo "compile_prepared - _prepared_parameter_types:".json_encode($this->_prepared_parameter_types)."<br />";
      // echo "compile_prepared - _prepared_parameter_keys:".json_encode($this->_prepared_parameter_keys)."<br />";
      //
      $stmt = $this->get_mysqli()->prepare($this->_compiled_query);
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
        $stmt->bind_param($types, ...$values);
      }
      //
      if ($stmt->execute()) {
        //
        while (
            ($result = $stmt->get_result()) ||
            ($stmt->more_results() && $stmt->next_result() && $result = $stmt->get_result())
          ) {
          $fields = array();
          while ($field = $result->fetch_field()) {
            array_push($fields, $field->name);
          }
          $list = AZList::create();
          while ($row = $result->fetch_array(MYSQLI_NUM)) {
            $rtn_data = AZData::create();
            //
            // echo "row:".json_encode($row)."<br />";
            // echo "fields:".json_encode($fields)."<br />";
            $idx = 0;
            foreach ($fields as $field) {
              $rtn_data->add($field, $row[$idx]);
              $idx++;
            }
            //
            $list->add($rtn_data);
          }
          // array_push($rtn_val, $list->to_json());
          array_push($rtn_val, $list);
          $result->free_result();
        }
        /*
        $result = $stmt->get_result();
        // echo "result:".json_encode($result->fetch_array(MYSQLI_NUM))."<br />";
        $fields = array();
        while ($field = $result->fetch_field()) {
          array_push($fields, $field->name);
        }
        $list = AZList::create();
        while ($row = $result->fetch_array(MYSQLI_NUM)) {
          $rtn_data = AZData::create();
          //
          // echo "row:".json_encode($row)."<br />";
          // echo "fields:".json_encode($fields)."<br />";
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
        //
        // $idx = 0;
        while ($stmt->more_results() && $stmt->next_result() && $result = $stmt->get_result()) {
          // echo "stmt more_results<br />";
          if ($result = $stmt->get_result()) {
            // echo "stmt more_results - result:".json_encode($result->fetch_array(MYSQLI_NUM))."<br />";
            $result->free_result();
          }
        }
        */
        //
        $stmt->free_result();
        $stmt->close();
      }
    }
    else {
      // prepared statement 가 아닌 일반 사용의 경우

      $this->compile();
      // echo "compile - query:".$this->get_query()."<br />";
      // echo "compile - _compiled_query:".$this->_compiled_query."<br />";
      //
      $result = $this->_db->query($this->_compiled_query);
      $list = $result->result_array();
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
      // echo "T:".mysqli_more_results($result->conn_id)."<br />";
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
      // echo "return - query: ".$query."<br />";
      $result = $this->get_mysqli()->query($query);
      if ($result && $result->num_rows > 0) {
        //
        $fields = array();
        while ($field = $result->fetch_field()) {
          $fields[$field->name] = $field->type;
        }
        // echo "return - fields: ".json_encode($fields)."<br />";
        //
        $result->data_seek(0);
        //
        $data = $result->fetch_array(MYSQLI_ASSOC);
        // echo "return - data: ".json_encode($data)."<br />";
        $result->free_result();
        //
        foreach ($keys as $key) {
          $key_mod = str_replace('@', 'o_', $key);
          $value = $data[$key_mod];
          // echo "return - key_mod: ".$key_mod." / value:".$value." / type:".$fields[$key_mod]."<br />";
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
    while(mysqli_more_results($results->conn_id) && mysqli_next_result($results->conn_id)) {
      if($l_result = mysqli_store_result($results->conn_id)) {
        mysqli_free_result($l_result);
      }
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