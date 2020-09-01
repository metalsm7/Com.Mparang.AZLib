<?php
class AZData implements Iterator {
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

  public function add(string $key, $value) {
    if (is_null($this->_data)) {
      $this->_data = array();
      $this->_keys = array();
    }
    $this->_data[$key] = $value;
    array_push($this->_keys, $key);
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
}

class AZList implements Iterator {
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
}

class AZSql {
  // protected $CI;
  private $_db;
  // private $_query;
  // private $_params;
  public function __construct(&$db = null) {
    // $this->CI =& get_instance();
    //
    if (!is_null($db)) {
      $this->_db = $db;
    }
    /*
    $query = 'SELECT * FROM bsg.admin_user WHERE au_key=@au_key AND au_name=@au_name';
    echo "query:{$query}\n";
    $key = '@au_key';
    $query = $this->param_replacer($query, '@au_key', 1);
    $query = $this->param_replacer($query, '@au_name', 'test_admin');
    echo "query:{$query}\n";
    */
  }

  public static function create(&$db = null) {
    return new AZSql($db);
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

  private function value_caster(&$value, $type) {
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
  }

  private function data_value_caster(array &$data, $metadata) {
    // $keys = array_keys($data);
    foreach ($data as $key => &$value) {
      foreach ($metadata as $meta) {
        if ($meta->name === $key) {
          $this->value_caster($value, $meta->type);
          break;
          /*
          switch ($meta->type) {
            case 'tinyint':
            case 'smallint':
            case 'mediumint':
            case 'int':
            case 'bigint':
              // echo "meta.".$key.".name:".$meta->name." / type:".$meta->type."\n";
              // echo "proc - ".$key.":".gettype($value);
              $value = intval($value);
              // echo " -> ".gettype($value)."\n";
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
      }
    }
  }

  public function __call($name, $args) {
    switch ($name) {
      case 'execute':
      case 'get':
      case 'get_data':
      case 'get_list':
        switch (count($args)) {
          case 1:
            return call_user_func_array(array($this, $name), $args);
          case 2:
            if (gettype($args[1]) === 'boolean') {
              return call_user_func_array(array($this, $name), $args);
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

  protected function execute(string $query, bool $identity = false): int {
    $rtn_val = 0;
    $this->_db->simple_query($query);
    $rtn_val = $identity ? $this->_db->insert_id() : $this->_db->affected_rows();
    return $rtn_val;
  }

  protected function execute_with_params(string $query, $params, bool $identity = false): int {
    if (gettype($params) === 'object' && $params instanceof AZData) {
      $params = $params->to_json();
    }
    //
    foreach ($params as $key => $value) {
      $query = $this->param_replacer($query, $key, $value);
    }
    //
    $rtn_val = -1;
    $q_result = $this->_db->simple_query($query);
    if ($q_result) {
      $rtn_val = $identity ? $this->_db->insert_id() : $this->_db->affected_rows();
    }
    return $rtn_val;
  }

  protected function get(string $query, $type_cast = false) {
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
    //
    return $rtn_val;
  }

  protected function get_with_params(string $query, $params, $type_cast = false) {
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
    //
    return $rtn_val;
  }

  /**
   * 지정된 쿼리 문자열에 대한 단일 행 결과를 AZData 객체로 반환
   * @param string $query 쿼리 문자열
   * @param bool $type_cast = false 결과값을 DB의 자료형에 맞춰서 type casting 할지 여부
   * @return AZData
   */
  protected function get_data(string $query, $type_cast = false): AZData {
    // echo "get_data.query:".$query."\n";
    $q_result = $this->_db->query($query);
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
    $q_result->free_result();
    // echo "get_data.data.POST:".json_encode($data)."\n";
    // return AZData::parse($this->_db->query($query)->row_array());
    return AZData::parse($data);
  }

  /**
   * 지정된 쿼리 문자열에 대한 단일 행 결과를 AZData 객체로 반환
   * @param string $query 쿼리 문자열
   * @param AZData|array $params 쿼리 문자열에 등록된 대체 문자열 자료
   * @param bool $type_cast = false 결과값을 DB의 자료형에 맞춰서 type casting 할지 여부
   * @return AZData
   */
  protected function get_data_with_params(string $query, $params, $type_cast = false): AZData {
    // echo "with_params - params.type:".gettype($params)."\n";
    // echo "with_params - params is instanceof:".($params instanceof AZData)."\n";
    if (gettype($params) === 'object' && $params instanceof AZData) {
      $params = $params->to_json();
    }
    //
    foreach ($params as $key => $value) {
      $query = $this->param_replacer($query, $key, $value);
    }
    // echo "with_params - query:".$query."\n";
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
    //
    $q_result->free_result();
    //
    // return AZData::parse($this->_db->query($query)->row_array());
    return AZData::parse($data);
  }

  /**
   * 지정된 쿼리 문자열에 대한 다행 결과를 AZList 객체로 반환
   */
  protected function get_list(string $query, $type_cast = false): AZList {
    //
    $rtn_val = new AZList();
    //
    $q_result = $this->_db->query($query);
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
    $q_result->free_result();
    return $rtn_val;
  }

  protected function get_list_with_params(string $query, $params, $type_cast = false) {
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
    $q_result = $this->_db->query($query);
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
    unset($params);
    unset($type_cast);
    //
    $q_result->free_result();
    //
    return $rtn_val;
  }
}
