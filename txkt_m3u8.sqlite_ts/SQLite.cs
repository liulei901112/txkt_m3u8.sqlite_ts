using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
namespace txkt_m3u8.sqlite_ts
{
    public class SQLite : IDisposable
	{
		private SQLiteConnection _connection;
		private SQLiteDataReader _reader;
		private SQLiteCommand _command;
		private string queryString;
		public SQLite(string path)
		{
			if (File.Exists(path))
			{
				_connection = new SQLiteConnection("data source = " + path);
				_connection.Open();
			}
			else
			{
				throw new Exception("db数据库不存在:" + path);
			}
		}
		// region IsExists        
		/// <summary>        
		/// 是否存在数据库        
		/// </summary>        
		/// <param name="path">目录</param>        
		/// <returns></returns>        
		public static bool IsExistsDateBase(string path)
		{
			if (Path.GetExtension(path).Equals(".sqlite"))
			{
				throw new SQLiteException("不是.sqlite后缀,你查什么!!!");
			}
			return File.Exists(path);
		}
		/// <summary>
		/// 表中是否存在字段
		/// </summary>
		/// <param name="tableName">表名</param>
		/// <param name="key">区分大小写</param>
		/// <returns></returns>        
		public bool IsExisTableFile(string tableName, string key)
		{
			bool isExis = false;
			foreach (var item in GetFiles(tableName))
			{
				{
					isExis = true;
					break;
				}
			}
			return isExis;
		}
		/// <summary>
		/// 查询表是否存在
		/// </summary>
		/// <param name="tableName">表名</param>
		/// <returns></returns>        
		public bool IsExistsTable(string tableName)
		{
			queryString = "SELECT name FROM sqlite_master WHERE name='" + tableName + "'";
			ExecuteQuery();
			if (!_reader.Read())
			{
				return false;
			}
			_reader.Close();
			return true;
		}
		// endregion                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           // region Create
		/// <summary>
		/// 创建数据库
		/// </summary>
		/// <param name="path">目录</param>        
		public static void CreateDateBase(string path)
		{
			if (Path.GetExtension(path).Equals(".sqlite"))
			{
				SQLiteConnection.CreateFile(path);
			}
			else
			{
				throw new Exception("要创建数据库，则文件后缀必须为.sqlite");
			}
		}
		// endregion        
		// region Delete
		/// <summary>
		/// 删除一张表
		/// </summary>
		/// <param name="tableName">表名</param>
		/// <returns>是否成功删除</returns>        
		public bool DeleteTable(string tableName)
		{
			if (IsExistsTable(tableName))
			{
				queryString = "DROP TABLE " + tableName;
				ExecuteQuery();
			}
			if (IsExistsTable(tableName))
			{
				return false;
			}
			return true;
		}
		/// <summary>
		/// 删除一行数据
		/// </summary>
		/// <param name="tableName">表名</param>
		/// <param name="key">key</param>
		/// <param name="value">值</param>        
		public void DeleteLine(string tableName, string key, string value)
		{
			queryString = "delete from " + tableName + " where " + key + " = " + "'" + value + "'";
			ExecuteQuery();
		}
		/// <summary>
		/// 删除数据库
		/// </summary>
		/// <param name="path"></param>        
		public void DeleteDateBase(string path)
		{
			if (File.Exists(path))
			{
				if (Path.GetExtension(path).Equals(".sqlite"))
				{
					File.Delete(path);
				}
				else
				{
					throw new FileNotFoundException("让你删除数据库文件，删啥呢");
				}
			}
		}
		// endregion         // region Get
		/// <summary>
		/// 表中多少列
		/// </summary>
		/// <param name="tableName">表名</param>
		/// <returns>列数</returns>        
		public int GetRowsCount(string tableName)
		{
			queryString = "Select count(*) From " + tableName;
			ExecuteQuery();
			_reader.Read();
			return _reader.GetInt32(0);
		}
		/// <summary>
		/// 获取数据库中表数量
		/// </summary>
		/// <returns>返回数据库中表数量</returns>        
		public int GetTablesCount()
		{
			return GetTablesName().Length;
		}
		/// <summary>
		/// 获取表中字段都有那些
		/// </summary>
		/// <param name="tableName">表名</param>
		/// <returns>字段数组</returns>        
		public string[] GetFiles(string tableName)
		{
			queryString = "Pragma Table_Info(" + tableName + ")";
			ExecuteQuery();
			List<string> tablesFiles = new List<string>();
			while (_reader.Read())
			{
				tablesFiles.Add(_reader["Name"].ToString());
			}
			return tablesFiles.ToArray();
		}
		/// <summary>
		/// 获取数据库中所有表名
		/// </summary>
		/// <returns>所有表名数组</returns>        
		public string[] GetTablesName()
		{
			queryString = "select name from sqlite_master where type='table' order by name";
			ExecuteQuery();
			List<string> tablesName = new List<string>();
			while (_reader.Read())
			{
				// 表名
				tablesName.Add(_reader["Name"].ToString());
			}
			return tablesName.ToArray();
		}
		/// <summary>
		/// 获取表中一列
		/// </summary>
		/// <param name="tableName">表名</param>
		/// <param name="fileName">字段名</param>
		/// <returns>列数组</returns>        
		public string[] GetRows(string tableName, string fileName)
		{
			if (!IsExistsTable(tableName))
			{
				throw new FileNotFoundException("表不存在:" + tableName);
			}
			if (!IsExisTableFile(tableName, fileName))
			{
				throw new Exception("表中不存在字段:" + fileName);
			}
			SQLiteDataAdapter sda = new SQLiteDataAdapter("select " + fileName + " from " + tableName, _connection);
			DataTable dt = new DataTable();
			sda.Fill(dt);
			string[] rows = new string[dt.Rows.Count];
			for (int i = 0; i < dt.Rows.Count; i++)
			{
				rows[i] = dt.Rows[i][fileName].ToString();
			}
			sda.Dispose();
			dt.Dispose();
			return rows;
		}
		/// <summary>
		/// 获取行数据，可能会有同名字段，会返回多条数据
		/// </summary>
		/// <param name="tableName">表名</param>
		/// <param name="key"></param>
		/// <param name="keyValue"></param>
		/// <returns></returns>        
		public List<string[]> GetLines(string tableName, string key, string keyValue)
		{
			queryString = string.Format("select * from {0} where {1} = '{2}'", tableName, key, keyValue);
			ExecuteQuery();
			string[] array = new string[_reader.FieldCount];
			List<string[]> result = new List<string[]>();
			while (_reader.Read())
			{
				for (int i = 0; i < _reader.FieldCount - 1; i++)
				{
					array[i] = Convert.ToString(_reader[i]);
				}
				result.Add(array);
			}
			return result;
		}
		public List<string[]> GetAllLines(string tableName)
		{
			queryString = string.Format("select * from {0}", tableName);
			ExecuteQuery();
			string[] array = new string[_reader.FieldCount];
			List<string[]> result = new List<string[]>();
			while (_reader.Read())
			{
				for (int i = 0; i < _reader.FieldCount - 1; i++)
				{
					array[i] = Convert.ToString(_reader[i]);
				}
				result.Add(array);
			}
			return result;
		}
		// endregion         
		// region Update
		/// <summary>
		/// 更新某一行的，某一个值
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="key"></param>
		/// <param name="keyVale"></param>
		/// <param name="updateKey">更新key</param>
		/// <param name="updateValue"></param>        
		public void UpdatePropety(string tableName, string key, string keyVale, string updateKey, string updateValue)
		{
			queryString = string.Format("UPDATE {0} SET {1} = '{2}' WHERE {3} = '{4}'", tableName, updateKey, updateValue, key, keyVale);
			ExecuteQuery();
		}
		/// <summary>
		/// 列数据为同一个
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>        
		public bool UpdateColumns(string tableName, string updateKey, string UpdateValue)
		{
			if (!IsExistsTable(tableName))
			{
				throw new FileNotFoundException("没有表: " + tableName);
			}
			if (!IsExisTableFile(tableName, updateKey))
			{
				throw new Exception("没有关键key:" + updateKey);
			}
			queryString = "UPDATE " + tableName + " SET " + updateKey + " = '" + UpdateValue + "'";
			ExecuteQuery();
			foreach (var item in GetRows(tableName, updateKey))
			{
				if (!item.Equals(UpdateValue))
				{
					return false;
				}
			}
			return true;
		}
		// endregion         // region Insert
		/// <summary>
		/// 表中插入一列
		/// </summary>
		/// <param name="tableName">表名</param>
		/// <param name="key">key</param>
		/// <returns>是否操作成功</returns>        
		public bool InsertRows(string tableName, string key, SQLiteType sQLiteType)
		{
			if (IsExistsTable(tableName))
			{
				if (IsExisTableFile(tableName, key))
				{
					throw new Exception("key值已存在,不可重复添加 :" + key);
				}
				else
				{
					queryString = "alter table " + tableName + " add column " + key + " " + sQLiteType.ToString();
					ExecuteQuery();
				}
			}
			else
			{
				throw new NullReferenceException("不存在表名:" + tableName);
			}
			if (IsExisTableFile(tableName, key))
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		// endregion         // region Rename
		/// <summary>
		/// 重命名表名
		/// </summary>
		/// <param name="tableName">要修改的表名</param>
		/// <param name="newName">新表名</param>
		/// <returns>是否重命名成功</returns>        
		public bool RenameTable(string tableName, string newName)
		{
			if (IsExistsTable(tableName))
			{
				queryString = "ALTER TABLE " + tableName + " RENAME TO " + newName;
				ExecuteQuery();
			}
			else
			{
				throw new FileNotFoundException("无法重命名不存在的表:" + tableName);
			}
			if (IsExistsTable(newName))
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		/// <summary>
		/// 重命名字段
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="fileName"></param>
		/// <param name="newFileName"></param>
		/// <returns></returns>        
		public bool RenameTableFile(string tableName, string fileName, string newFileName)
		{
			if (!IsExistsTable(tableName))
			{
				throw new FileNotFoundException("表不存在:" + tableName);
			}
			return false;
		}
		// endregion         
		// region Todo        
		//创建表        
		//插入行        
		//删除列         
		//获取第N行、第一行、最后一行数据                
		public void CreateTables(string tables)
		{
		}
		public void InsertLines(string tablesName, string[] values)
		{
		}
		/// <summary>
		/// 删除列
		/// </summary>
		/// <param name="key"></param>        
		public void DeleteRows(string key)
		{
		}
		// endregion        
		public void Dispose()
		{
			_connection?.Close();
			_connection?.Dispose();
			_reader?.Dispose();
			_command?.Dispose();
		}
		private void ExecuteQuery()
		{
			_command = _connection.CreateCommand();
			_command.CommandText = queryString;
			_reader = _command.ExecuteReader();
		}
		public enum SQLiteType
		{
			blob, integer, varchar, text,
		}
	}
}