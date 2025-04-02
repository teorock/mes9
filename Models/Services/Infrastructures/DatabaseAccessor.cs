using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using mes.Models.ViewModels;
using Microsoft.Data.Sqlite;

namespace mes.Models.Services.Infrastructures
{    
    public class DatabaseAccessor
    {
        private bool LogEnabled = false;
        private string logConnectionString = "Data Source=../mesData/dbmovements.db";

        // versione 2.0 - 12 giu 2024


        const string logPath = @"c:\temp\intranet.log";        

        public int Insertor<T>(string connectionString, string tableName, object inputObject)
        {
            Log2File("insertor");
            var properties = typeof(T).GetProperties();
            List<string> allProperties = new List<string>();
            List<string> allValues = new List<string>();

            foreach(var property in properties)
            {
                allProperties.Add(property.Name);
                allValues.Add(property.GetValue(inputObject).ToString());   
            }

            string propertiesLine ="";
            foreach(var oneProp in allProperties)
            {
                propertiesLine += $"'{oneProp}',";
            }

            string valuesLine ="";
            foreach(var oneValue in allValues)
            {
                valuesLine += $"'{InputValidator(oneValue)}',";
            }

            if(LogEnabled)
            {
                LogDbMovement(connectionString, tableName, inputObject, inputObject, "Create", 0);
            }

            string query = $"INSERT INTO {tableName} ({propertiesLine.Substring(0,propertiesLine.Length-1)}) VALUES ({valuesLine.Substring(0,valuesLine.Length-1)})";
            Log2File(query);
            
            try
            {
                var conn = new SqliteConnection(connectionString);
                conn.Open();

                var cmd= new SqliteCommand(query, conn);
                cmd.ExecuteNonQuery();  

                conn.Close();
                conn.Dispose();
            }
            catch(Exception excp)
            {
                Log2File($"ERRORE: {excp.Message}");
                return 1;
            }
            return 0;
        }

        public int Updater<T>(string connectionString, string tableName, object inputObject, long id)
        {
            Log2File("updater");
            var properties = typeof(T).GetProperties();
            List<string> allProperties = new List<string>();
            List<string> allValues = new List<string>();

            foreach(var property in properties)
            {
                allProperties.Add(property.Name);
                allValues.Add(property.GetValue(inputObject).ToString());   
            }

            string propertiesLine ="";
            foreach(var oneProp in allProperties)
            {
                propertiesLine += $"'{oneProp}',";
            }

            string valuesLine ="";
            foreach(var oneValue in allValues)
            {
                valuesLine += $"'{InputValidator(oneValue)}',";
            }

            if(LogEnabled)
            {
                List<T> allOriginal = Queryer<T>(connectionString, tableName);

                object originalObject = new Object();
                foreach(T oneObj in allOriginal)
                {
                    long objId = (long)oneObj.GetType().GetProperty("id").GetValue(oneObj, null);
                    if (objId == id)
                    {
                        originalObject = oneObj;
                        break;
                    }
                }

                LogDbMovement(connectionString, tableName, inputObject, originalObject, "Update", id);
            }

            string query = $"UPDATE {tableName} SET ({propertiesLine.Substring(0,propertiesLine.Length-1)}) = ({valuesLine.Substring(0,valuesLine.Length-1)}) WHERE id={id}";
            Log2File(query);

            try
            {
                var conn = new SqliteConnection(connectionString);
                conn.Open();

                var cmd= new SqliteCommand(query, conn);
                cmd.ExecuteNonQuery();  

                conn.Close();
                conn.Dispose();
            }
            catch (Exception excp)
            {
                Log2File($"ERRORE: {excp.Message}");
            }
            return 0;
        }

        public List<T> Queryer<T>(string connectionString, string table)
        {
            Log2File("queryer");
            var typeOfInstance = typeof(T);

            List<T> results = new List<T>();

            var properties = typeof(T).GetProperties();

            try
            {
                var conn = new SqliteConnection(connectionString);
                conn.Open();

                var cmd2 = new SqliteCommand($"SELECT * FROM {table}", conn);
                Log2File(cmd2.CommandText);
                var reader = cmd2.ExecuteReader();
                
                while(reader.Read())
                {
                    var instance = Activator.CreateInstance(typeof(T));
                    
                    foreach(var oneProp in properties)
                    {
                        try
                        {
                            typeOfInstance.GetProperty(oneProp.Name).SetValue(instance, reader[oneProp.Name]);
                        }
                        catch (Exception excp)
                        {
                            Log2File($"ERRORE: {excp.Message}");
                        }
                    }
                    results.Add((T)instance);
                    
                } 

                conn.Close();
                conn.Dispose();
            }
            catch (Exception excp)
            {
                Log2File($"ERRORE: {excp.Message}");
            }

            return results;

        }                

        public List<T> QueryerFilter<T>(string connectionString, string table, string filter)
        {
            Log2File("queryerFilter");
            var typeOfInstance = typeof(T);

            List<T> results = new List<T>();

            var properties = typeof(T).GetProperties();

            try
            {
                var conn = new SqliteConnection(connectionString);
                conn.Open();

                var cmd2 = new SqliteCommand($"SELECT * FROM {table} WHERE {filter}", conn);
                Log2File(cmd2.CommandText);
                var reader = cmd2.ExecuteReader();
                
                while(reader.Read())
                {
                    var instance = Activator.CreateInstance(typeof(T));
                    
                    foreach(var oneProp in properties)
                    {
                        try
                        {
                            typeOfInstance.GetProperty(oneProp.Name).SetValue(instance, reader[oneProp.Name]);
                        }
                        catch (Exception excp)
                        {
                            Log2File($"ERRORE: {excp.Message}");
                        }
                    }
                    results.Add((T)instance);
                    
                } 

                conn.Close();
                conn.Dispose();
            }
            catch (Exception excp)
            {
                Log2File($"ERRORE: {excp.Message}");
            }
            return results;
        }                

        public List<T> QueryerCommand<T>(string connectionString, string table, string command)
        {
            Log2File("queryerCommand");
            var typeOfInstance = typeof(T);

            List<T> results = new List<T>();

            var properties = typeof(T).GetProperties();

            try
            {
                var conn = new SqliteConnection(connectionString);
                conn.Open();

                var cmd2 = new SqliteCommand(command, conn);
                Log2File(cmd2.CommandText);
                var reader = cmd2.ExecuteReader();
                
                while(reader.Read())
                {
                    var instance = Activator.CreateInstance(typeof(T));
                    
                    foreach(var oneProp in properties)
                    {
                        try
                        {
                            typeOfInstance.GetProperty(oneProp.Name).SetValue(instance, reader[oneProp.Name]);
                        }
                        catch (Exception excp)
                        {
                            Log2File($"ERRORE: {excp.Message}");
                        }
                    }
                    results.Add((T)instance);
                    
                } 

                conn.Close();
                conn.Dispose();
            }
            catch (Exception excp)
            {
                Log2File($"ERRORE: {excp.Message}");
            }
            return results;
        }  

        public int Delete (string connectionString, string table, long id)
        {
            int result = 0;
            try
            {
                var conn = new SqliteConnection(connectionString);
                conn.Open();

                var cmd2 = new SqliteCommand($"DELETE FROM {table} WHERE id={id}", conn);
                var reader = cmd2.ExecuteReader();

                conn.Close();
                conn.Dispose();
            }
            catch (Exception excp)
            {
                Log2File($"ERRORE: {excp.Message}");
                return -1;
            }

            return result;            
        }

        public string InputValidator(string inputString)
        {
            List<char> unwanted = new List<char>(){';', '(', ')', '=', '*', '\''};
            List<string> toBeReplaced = new List<string>(){" OR", "1=1", "DROP", "SELECT", "INSERT", "INTO", "UPDATE", "DATABASE", "TABLE", "WHERE", "\"\""};

            string tempString="";
            //per singolo carattere
            for(int x=0;x<inputString.Length;x++)
            {
                if(unwanted.Contains(inputString[x]))
                {
                    tempString+='_';
                } else {
                    tempString += inputString[x];
                }
            }

            string tempString2="";

            foreach(var oneToken in toBeReplaced)
            {
                if (tempString.Contains(oneToken))
                {
                    tempString2 = Regex.Replace(tempString, oneToken, "xxx");
                    tempString = tempString2;
                }                
            }
            return tempString;
        }

        public string GetDbNameFromConnString(string connString)
        {
            string[] parts = connString.Split('/');

            return parts[parts.Count()-1];
        }
        
        private void LogDbMovement(string connectionString, string tableName, object inputObject, object actualObject, string operationType, long id)
        {
            List<DbModification> allChanges = new List<DbModification>();

            string logQuery ="";

            string dbName =  GetDbNameFromConnString(connectionString);
            string dbTable = tableName;

            //---- CODICE ----            
            string code = "codice";
            try
            {
                code = inputObject.GetType().GetProperty("Codice").GetValue(inputObject, null).ToString();
            }
            catch (Exception excp)
            {                
                code = $"no code - error: {excp.Message}";
                Log2File($"ERRORE: {code}");
            }

            //---- descrizione ----            
            string description = "descrizione";
            try
            {
                description = inputObject.GetType().GetProperty("Nome").GetValue(inputObject, null).ToString();
            }
            catch (Exception excp)
            {
                description = $"no name/description - error:{excp.Message}";
                Log2File($"ERRORE: {description}");
            }

            string user = inputObject.GetType().GetProperty("CreatedBy").GetValue(inputObject, null).ToString();
            string modifiedOn = inputObject.GetType().GetProperty("CreatedOn").GetValue(inputObject, null).ToString();
            
            if (operationType == "Create")
            {
                allChanges.Add(new DbModification {
                    DbColumn = "none",
                    PreviousValue ="0",
                    NewValue ="0"
                });
            }
            if (operationType == "Update")
            {
                allChanges = ObjectComparer(inputObject, actualObject);             
            }

            foreach(DbModification oneMod in allChanges)
            {
                logQuery = $"INSERT INTO Main (DbName, DbTable, DbColumn, Code, Description, OperationType, PreviousVal, NewVal, User, ModifiedOn)" 
                                + $"VALUES ('{dbName}', '{dbTable}', '{oneMod.DbColumn}', '{code}', '{description}','{operationType}', '{oneMod.PreviousValue}', '{oneMod.NewValue}', '{user}', '{modifiedOn}')";
            }

            if(logQuery!="")
            {
                var logConn = new SqliteConnection(logConnectionString);
                logConn.Open();

                var logCmd= new SqliteCommand(logQuery, logConn);            
                logCmd.ExecuteNonQuery();  

                logConn.Close();
                logConn.Dispose();   
            }
        }

        private List<DbModification> ObjectComparer(object inputObj, object actualObj)
        {
            List<DbModification> result = new List<DbModification>();

            var inputProps = inputObj.GetType().GetProperties();

            foreach(var oneProp in inputProps)
            {
                if(oneProp.Name !="CreatedBy" && oneProp.Name !="CreatedOn")
                {
                    var inputVal = inputObj.GetType().GetProperty(oneProp.Name).GetValue(inputObj, null).ToString();
                    var actualVal = actualObj.GetType().GetProperty(oneProp.Name).GetValue(actualObj, null).ToString();

                    if(inputVal!= actualVal)
                    {
                        DbModification oneMod = new DbModification() {
                            DbColumn = oneProp.Name,
                            PreviousValue = actualVal,
                            NewValue = inputVal
                        };

                        result.Add(oneMod);
                    }
                }
            }
            return result;
        }
        
        public bool CheckDoubleRecord(List<object> inputList, object one2check)
        {
            foreach(object oneObj in inputList)
            {
                List<DbModification> result = ObjectComparer(oneObj, one2check);
                if(result.Count == 0) return false;
            }

            return false;
        }

        private void Log2File(string line2log)
        {
            using(StreamWriter sw = new StreamWriter(logPath, true))
            {
                sw.WriteLine($"{DateTime.Now} -> {line2log}");
            }
        }
    }
}