using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;

namespace HHCSService
{
    public class AuthHeader : SoapHeader
    {
        private readonly string remoteAccess = "c2VydmVyPTEyNy4wLjAuMTtwb3J0PTMzMDY7ZGF0YWJhc2U9Y2tkO1VzZXIgSWQ9cm9vdDtQYXNzd29yZD1sb3ZlbG92ZTEyO2NoYXJzZXQ9dXRmOA==";
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UserValidation()
        {
            var mySQLConn = new MySqlConnection(EncodeHelper.Base64Decode(remoteAccess));
            mySQLConn.Open();
            var mySQLCommand = mySQLConn.CreateCommand();
            mySQLCommand.CommandText = $"SELECT ud_pass FROM UserTABLE WHERE ud_email = '{Username}'";
            if (ComparePassword(Password, (string)mySQLCommand.ExecuteScalar()))
            {
                return true;
            }
            return false;
        }
        private bool ComparePassword(string password, string passwordHash)
        {
            try
            {
                byte[] hashBytes = Convert.FromBase64String(passwordHash);
                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);
                var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
                byte[] hash = pbkdf2.GetBytes(20);
                for (int i = 0; i < 20; i++)
                {
                    if (hashBytes[i + 16] != hash[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    /// <summary>
    /// Summary description for HHCSService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class HHCSService : System.Web.Services.WebService
    {
        private readonly string remoteAccess = "c2VydmVyPTEyNy4wLjAuMTtwb3J0PTMzMDY7ZGF0YWJhc2U9Y2tkO1VzZXIgSWQ9cm9vdDtQYXNzd29yZD1sb3ZlbG92ZTEyO2NoYXJzZXQ9dXRmOA==";
        private string CreatePasswordHash(string password)
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);
            return Convert.ToBase64String(hashBytes);
        }
        private bool ComparePassword(string password, string passwordHash)
        {
            try
            {
                byte[] hashBytes = Convert.FromBase64String(passwordHash);
                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);
                var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
                byte[] hash = pbkdf2.GetBytes(20);
                for (int i = 0; i < 20; i++)
                {
                    if (hashBytes[i + 16] != hash[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Register user on MySQL database side.
        /// </summary>
        /// <param name="ud_email"></param>
        /// <param name="ud_pass"></param>
        /// <returns>return array of object where index 0 is maximum id and index 1 is hash of password.</returns>
        [WebMethod]
        //[SoapHeader("Authentication", Required = true)]
        public object[] Register(string ud_email, string ud_pass, string ud_iden_number, string ud_gender, string ud_name, DateTime ud_datetime)
        {
            object[] ReturnData = new object[2];
            try
            {
                var mConn = new MySqlConnection(EncodeHelper.Base64Decode(remoteAccess));
                mConn.Open();
                var command = mConn.CreateCommand();
                command.CommandText = $@"
                    INSERT INTO `ckd`.`usertable`
                        (`ud_id`,
                        `ud_email`,
                        `ud_pass`,
                        `ud_iden_number`,
                        `ud_gender`,
                        `ud_name`,
                        `ud_birthdate`)
                        VALUES
                        (NULL,
                        '{ud_email}',
                        '{CreatePasswordHash(ud_pass)}',
                        '{ud_iden_number}',
                        '{ud_gender}',
                        '{ud_name}',
                        '{ud_datetime.ToString("yyyy-MM-dd HH:mm:ss")}');
                ";
                command.ExecuteNonQuery();
                command.CommandText = "SELECT MAX(ud_id) FROM UserTABLE";
                ReturnData[0] = (int)command.ExecuteScalar();
                command.CommandText = $"SELECT ud_pass FROM UserTABLE WHERE ud_email = '{ud_email}'";
                ReturnData[1] = (string)command.ExecuteScalar();
                return ReturnData;
            }
            catch (Exception ex)
            {
                return new object[] { ex.Message };
                //return null;
            }
        }
        /// <summary>
        /// Test service connection to determine if the connection is availble.
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        //[SoapHeader("Authentication", Required = true)]
        public bool TestConnection(AuthHeader Authentication)
        {
            if (Authentication == null)
                throw new UnauthorizedAccessException();
            if (Authentication.UserValidation())
                return true;
            return false;
        }
        [WebMethod]
        //[SoapHeader("Authentication", Required = true)]
        public DataSet GetFoodExchangeData(AuthHeader Authentication, int id)
        {
            if (Authentication == null || !Authentication.UserValidation())
                throw new UnauthorizedAccessException();
            try
            {
                var query = $@"SELECT * FROM FoodTABLE WHERE food_id IN (SELECT foodexchange_id FROM foodexchangetable WHERE food_id = {id})";
                var mySQLConn = new MySqlConnection(EncodeHelper.Base64Decode(remoteAccess));
                mySQLConn.Open();
                var tickets = new DataSet();
                var adapter = new MySqlDataAdapter(query, mySQLConn);
                adapter.Fill(tickets, "FoodTABLE");
                mySQLConn.Close();
                return tickets;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        /// <summary>
        /// Get FoodTABLE data in form of DataSet
        /// </summary>
        /// <returns></returns>
        [WebMethod]
        //[SoapHeader("Authentication", Required = true)]
        public DataSet GetFoodData(AuthHeader Authentication, string search_query)
        {
            if (Authentication == null || !Authentication.UserValidation())
                throw new UnauthorizedAccessException();
            try
            {
                var query = string.Empty;
                if (string.IsNullOrEmpty(search_query))
                    query = "SELECT * FROM FoodTABLE";
                else
                    query = $@"SELECT * FROM FoodTABLE WHERE food_name LIKE '%{search_query}%'";
                var mySQLConn = new MySqlConnection(EncodeHelper.Base64Decode(remoteAccess));
                mySQLConn.Open();
                var tickets = new DataSet();
                var adapter = new MySqlDataAdapter(query, mySQLConn);
                adapter.Fill(tickets, "FoodTABLE");
                mySQLConn.Close();
                return tickets;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Get data from any table with related username and password
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [WebMethod]
        //[SoapHeader("Authentication", Required = true)]
        public DataSet GetData(AuthHeader Authentication, string tableName)
        {
            if (Authentication == null || !Authentication.UserValidation())
                throw new UnauthorizedAccessException();
            try
            {
                var mySQLConn = new MySqlConnection(EncodeHelper.Base64Decode(remoteAccess));
                mySQLConn.Open();
                var mySQLCommand = mySQLConn.CreateCommand();
                mySQLCommand.CommandText = $"SELECT ud_pass FROM UserTABLE WHERE ud_email = '{Authentication.Username}'";
                if (ComparePassword(Authentication.Password, (string)mySQLCommand.ExecuteScalar()))
                {
                    List<object> returnData = new List<object>();
                    var query = 
                            $"SELECT * FROM {tableName} " +
                            $"WHERE ud_id = (SELECT ud_id FROM UserTABLE " +
                            $"               WHERE ud_email = '{Authentication.Username}')";
                    var tickets = new DataSet();
                    var adapter = new MySqlDataAdapter(query, mySQLConn);
                    adapter.Fill(tickets, tableName);
                    /*
                    foreach (Data((TEMP_DiabetesTABLE)row) ((TEMP_DiabetesTABLE)row) in tickets.Tables[tableName].((TEMP_DiabetesTABLE)row)s)
                    {
                        returnData.Add(((TEMP_DiabetesTABLE)row));
                    }
                    */
                    return tickets;
                }
                mySQLConn.Close();
                return null;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Synchronize offline database with this online database, use only data in temporary table from sqlite trigger AND not manual the data yourself.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="tempDiabetes"></param>
        /// <param name="tempKidney"></param>
        /// <param name="tempPressure"></param>
        /// <returns></returns>
        [WebMethod]
        //[SoapHeader("Authentication", Required = true)]
        public string[] SynchonizeData(AuthHeader Authentication, List<TEMP_DiabetesTABLE> tempDiabetes, List<TEMP_KidneyTABLE> tempKidney, List<TEMP_PressureTABLE> tempPressure)
        {
            if (Authentication == null || !Authentication.UserValidation())
                throw new UnauthorizedAccessException();
            object result = null;
            List<string> queryList = new List<string>();
            queryList.Add("START");
            try
            {
                var mySQLConn = new MySqlConnection(EncodeHelper.Base64Decode(remoteAccess));
                mySQLConn.Open();
                var mySQLCommand = mySQLConn.CreateCommand();
                mySQLCommand.CommandText = $"SELECT ud_pass FROM UserTABLE WHERE ud_email = '{Authentication.Username}'";
                if (ComparePassword(Authentication.Password, (string)mySQLCommand.ExecuteScalar()))
                {
                    queryList.Add("ENTER");
                    queryList.Add($"Size of D{tempDiabetes.ToList().Count} K{tempKidney.ToList().Count} P{tempPressure.ToList().Count}");
                    mySQLCommand.CommandText = $"SELECT ud_id FROM UserTABLE WHERE ud_email = '{Authentication.Username}'";
                    var userID = (int)mySQLCommand.ExecuteScalar();
                    tempDiabetes.ForEach(row =>
                    {
                        if (((TEMP_DiabetesTABLE)row).mode == "I")
                        {
                            mySQLCommand.CommandText =
                            $"INSERT INTO ckd.DiabetesTABLE " +
                            $"values({((TEMP_DiabetesTABLE)row).fbs_id_pointer}" +
                            $",'{((TEMP_DiabetesTABLE)row).fbs_time_new.ToString("yyyy-MM-dd HH:mm:ss")}'" +
                            $",{((TEMP_DiabetesTABLE)row).fbs_fbs_new}" +
                            $",{((TEMP_DiabetesTABLE)row).fbs_fbs_lvl_new}" +
                            $",{userID})";
                        }
                        else if (((TEMP_DiabetesTABLE)row).mode == "U")
                        {
                            mySQLCommand.CommandText =
                            $@"UPDATE ckd.DiabetesTABLE 
                                SET
                                    fbs_fbs = {row.fbs_fbs_new}
                                    ,fbs_time = '{row.fbs_time_string_new}'
                                    ,fbs_fbs_lvl = {row.fbs_fbs_lvl_new}
                                WHERE 
                                    fbs_id = {row.fbs_id_pointer}
                                AND
                                    ud_id = {userID};
                                ";
                        }
                        else if (((TEMP_DiabetesTABLE)row).mode == "D")
                        {
                            mySQLCommand.CommandText =
                            $@"DELETE FROM ckd.DiabetesTABLE where fbs_id = {((TEMP_DiabetesTABLE)row).fbs_id_pointer} AND ud_id = {userID};";
                        }
                        try
                        {
                            queryList.Add(mySQLCommand.CommandText);
                            mySQLCommand.ExecuteNonQuery();

                        }
                        catch (Exception e)
                        {
                            result = e.Message;
                            queryList.Add(e.Message);
                        }
                    });
                    tempKidney.ForEach(row =>
                    {
                        if (((TEMP_KidneyTABLE)row).mode == "I")
                        {
                            mySQLCommand.CommandText = $"" +
                            $"INSERT INTO ckd.KidneyTABLE " +
                            $"values(" +
                            $"{((TEMP_KidneyTABLE)row).ckd_id_pointer}" +
                            $",'{((TEMP_KidneyTABLE)row).ckd_time_new.ToString("yyyy-MM-dd HH:mm:ss")}'" +
                            $",{((TEMP_KidneyTABLE)row).ckd_gfr_new}" +
                            $",{((TEMP_KidneyTABLE)row).ckd_gfr_level_new}" +
                            $",{((TEMP_KidneyTABLE)row).ckd_creatinine_new}" +
                            $",{((TEMP_KidneyTABLE)row).ckd_bun_new}" +
                            $",{((TEMP_KidneyTABLE)row).ckd_sodium_new}" +
                            $",{((TEMP_KidneyTABLE)row).ckd_potassium_new}" +
                            $",{((TEMP_KidneyTABLE)row).ckd_albumin_blood_new}" +
                            $",{((TEMP_KidneyTABLE)row).ckd_albumin_urine_new}" +
                            $",{((TEMP_KidneyTABLE)row).ckd_phosphorus_blood_new}" +
                            $",{userID})";
                        }
                        else if (((TEMP_KidneyTABLE)row).mode == "U")
                        {
                            mySQLCommand.CommandText =
                            $@"UPDATE ckd.KidneyTABLE 
                        SET
                            ckd_time        = '{((TEMP_KidneyTABLE)row).ckd_time_string_new}'
                            ,ckd_gfr        = {((TEMP_KidneyTABLE)row).ckd_gfr_new}
                            ,ckd_gfr_level  = {((TEMP_KidneyTABLE)row).ckd_gfr_level_new}
                            ,ckd_creatinine = {((TEMP_KidneyTABLE)row).ckd_creatinine_new}
                            ,ckd_bun        = {((TEMP_KidneyTABLE)row).ckd_bun_new}
                            ,ckd_sodium     = {((TEMP_KidneyTABLE)row).ckd_sodium_new}
                            ,ckd_potassium  = {((TEMP_KidneyTABLE)row).ckd_potassium_new}
                            ,ckd_albumin_blood = {((TEMP_KidneyTABLE)row).ckd_albumin_blood_new}
                            ,ckd_albumin_urine = {((TEMP_KidneyTABLE)row).ckd_albumin_urine_new}
                            ,ckd_phosphorus_blood = {((TEMP_KidneyTABLE)row).ckd_phosphorus_blood_new}
                        WHERE 
                            ckd_id = {((TEMP_KidneyTABLE)row).ckd_id_pointer}
                        AND
                            ud_id = {userID};
                        ";
                        }
                        else if (((TEMP_KidneyTABLE)row).mode == "D")
                        {
                            mySQLCommand.CommandText =
                            $@"DELETE FROM ckd.KidneyTABLE where ckd_id = {((TEMP_KidneyTABLE)row).ckd_id_pointer} AND ud_id = {userID};";
                        }
                        try
                        {
                            queryList.Add(mySQLCommand.CommandText);
                            mySQLCommand.ExecuteNonQuery();

                        }
                        catch (Exception e)
                        {
                            result = e.Message;
                            queryList.Add(e.Message);
                        }
                    });
                    tempPressure.ForEach(row =>
                    {
                        if (((TEMP_PressureTABLE)row).mode == "I")
                        {
                            mySQLCommand.CommandText =
                            $"INSERT INTO ckd.PressureTABLE " +
                            $"values(" +
                            $"{((TEMP_PressureTABLE)row).bp_id_pointer}" +
                            $",'{((TEMP_PressureTABLE)row).bp_time_new.ToString("yyyy-MM-dd HH:mm:ss")}'" +
                            $",{((TEMP_PressureTABLE)row).bp_up_new}" +
                            $",{((TEMP_PressureTABLE)row).bp_lo_new}" +
                            $",{((TEMP_PressureTABLE)row).bp_hr_new}" +
                            $",{((TEMP_PressureTABLE)row).bp_up_lvl_new}" +
                            $",{((TEMP_PressureTABLE)row).bp_lo_lvl_new}" +
                            $",{((TEMP_PressureTABLE)row).bp_hr_lvl_new}" +
                            $",{userID}" +
                            $")";
                        }
                        else if (((TEMP_PressureTABLE)row).mode == "U")
                        {
                            mySQLCommand.CommandText =
                            $@"UPDATE ckd.PressureTABLE
                            SET
                               bp_time  = '{((TEMP_PressureTABLE)row).bp_time_string_new}'
                              ,bp_up    = {((TEMP_PressureTABLE)row).bp_up_new}
                              ,bp_lo    = {((TEMP_PressureTABLE)row).bp_lo_new}
                              ,bp_hr    = {((TEMP_PressureTABLE)row).bp_hr_new}
                              ,bp_up_lvl = {((TEMP_PressureTABLE)row).bp_up_lvl_new}
                              ,bp_lo_lvl = {((TEMP_PressureTABLE)row).bp_lo_lvl_new}
                              ,bp_hr_lvl = {((TEMP_PressureTABLE)row).bp_hr_lvl_new}
                            WHERE
                                bp_id = {((TEMP_PressureTABLE)row).bp_id_pointer}
                            AND
                                ud_id = {userID}
                        ";
                        }
                        else if (((TEMP_PressureTABLE)row).mode == "D")
                        {
                            mySQLCommand.CommandText =
                            $@"DELETE FROM ckd.PressureTABLE 
                            WHERE 
                                bp_id = {((TEMP_PressureTABLE)row).bp_id_pointer} 
                            AND 
                                ud_id = {userID};";
                        }
                        try
                        {
                            queryList.Add(mySQLCommand.CommandText);
                            mySQLCommand.ExecuteNonQuery();

                        }
                        catch (Exception e)
                        {
                            result = e.Message;
                            queryList.Add(e.Message);
                        }
                    });
                }
                mySQLConn.Close();
            }
            catch (Exception e)
            {
                queryList.Add(e.Message);
            }
            return queryList.ToArray();
        }
        /// <summary>
        /// Only for data structure generating.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="data2"></param>
        /// <param name="data3"></param>
        /// <returns></returns>
        [WebMethod]
        //[SoapHeader("Authentication", Required = true)]
        public string ClassXMLGenerateTest(TEMP_DiabetesTABLE data, TEMP_KidneyTABLE data2, TEMP_PressureTABLE data3)
        {
            return "Success";
        }
        [WebMethod]
        //[SoapHeader("Authentication", Required = true)]
        public bool FoodRequest(AuthHeader Authentication, string food_name)
        {
            if (Authentication == null || !Authentication.UserValidation())
                throw new UnauthorizedAccessException();
            var mySQLConn = new MySqlConnection(EncodeHelper.Base64Decode(remoteAccess));
            mySQLConn.Open();
            var mySQLCommand = mySQLConn.CreateCommand();
            mySQLCommand.CommandText = $@"INSERT INTO temp_foodtable(food_name) values('{food_name}')";
            try
            {
                mySQLCommand.ExecuteNonQuery();
            }
            catch
            {
                return false;
            }
            finally
            {
                mySQLConn.Close();
            }
            return true;
        }
    }
}
