using freeBIM;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;

namespace LinqSample
{
    public partial class Form1 : Form
    {
        MetaData metaData;
        Rvt_parameter_mapping rvt_parameter_mapping = new Rvt_parameter_mapping();
        DataTable Subjects, Properties, Assigns_Properties, Assigns_Measures, Rvt_Guid_parameter, Measures;
        string xmlPath;
        Microsoft.Win32.RegistryKey regKey;
        string regKeyName = @"Software\CAD Anwendungen Muigg\Revit_Mapping";
        string metaDataName;
        string server, user, password, dbName;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            metaData = new MetaData();
            regKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regKeyName);
            if (regKey != null)
            {
                xmlPath = regKey.GetValue("XML_Path").ToString();
                metaDataName= xmlPath + "\\freeBimXML.xml";
                server= regKey.GetValue("Server").ToString();
                user= regKey.GetValue("User").ToString();
                password= regKey.GetValue("Password").ToString();
                dbName = regKey.GetValue("Database").ToString();
            }
        }

        // load mySQL and create XML
        private void button2_Click(object sender, EventArgs e)
        {
            metaData.Clear();
            getMySqlTables("freebim");
            dataGridView1.DataSource = metaData.Tables["Properties"];
            dataGridView2.DataSource = metaData.Tables["Measures"];
            dataGridView3.DataSource = metaData.Tables["Values"];
            dataGridView7.DataSource = metaData.Tables["Subjects"];
            dataGridView8.DataSource = metaData.Tables["Assigns_Properties"];
            metaData.WriteXml(metaDataName);
        }
        
        // load XML and remap parameters


        private void button1_Click(object sender, EventArgs e)
        {
            metaData.Clear();
            metaData.ReadXml(metaDataName);
            Subjects = metaData.Tables["Subjects"];
            Properties = metaData.Tables["Properties"];
            int nRows = Subjects.Rows.Count;
            Assigns_Properties = metaData.Tables["Assigns_Properties"];
            var results1 = from p in Properties.AsEnumerable()
                          join r in Assigns_Properties.AsEnumerable() on p.Field<string>("freeBIM_Guid") equals r.Field<string>("Guid_Property")
                          where r.Field<string>("Guid_Subject") == "1"
                          select new
                          {
                              name = (string)p["Name_Loc"],
                              phase = (string)r["Guid_Phase"],
                              desc= (string)p["Description_Loc"]
                          };


            DataTable result1 = new DataTable();
            result1.Columns.Add("Name");
            result1.Columns.Add("Description");
            result1.Columns.Add("Phase");

            foreach (var item in results1)
            {
                DataRow newRow = result1.NewRow();
                newRow["Name"] = item.name;
                newRow["Description"] = item.desc;
                newRow["Phase"] = item.phase;

                result1.Rows.Add(newRow);
                Console.WriteLine(item.name);

            }

            dataGridView4.DataSource = result1;

            // Measures für einen bestimmten Parameter/Kategorie
            // Tables: Assigns_Measures, Rvt_Guid_parameter, Measures,
            Assigns_Measures = metaData.Tables["Assigns_Measures"];
            Measures = metaData.Tables["Measures"];
            Rvt_Guid_parameter = rvt_parameter_mapping.Tables["Rvt_guid_parameter_mapping"];

            
            var results2 = from m in Measures.AsEnumerable()
                      join a in Assigns_Measures.AsEnumerable() on m.Field<string>("freeBIM_Guid") equals a.Field<string>("Guid_Measure")
                      join r in Rvt_Guid_parameter.AsEnumerable() on a.Field<string>("Guid_Property") equals r.Field<string>("freeBIM_Guid")
                      select new{
                          freeBIM_Guid = (string)r["freeBIM_Guid"],
                          rvt_ID = (string)r["rvt_Parameter_ID"],
                          Measure = (string)m["Name_Loc"]

                      };

            DataTable result2 = new DataTable();
            result2.Columns.Add("Name");
            result2.Columns.Add("freeBIM_Guid");
            foreach (var item in results2)
            {
                DataRow newRow = result2.NewRow();
                newRow["freeBIM_Guid"] = item.freeBIM_Guid;
                newRow["Name"] = item.Measure;

                result2.Rows.Add(newRow);
                Console.WriteLine(item.Measure);

            }

            dataGridView5.DataSource = result2;


        }

        DataTable getTable(string Database, string Tablename, string[] spalten, bool query, string filter)
        {
            MySqlConnection conn;
            string connString = "Server=" + server + ";Database=" + dbName + ";Uid=" + user + ";Pwd=" + password + ";ConvertZeroDateTime=true";
            try
            {
                conn = new MySql.Data.MySqlClient.MySqlConnection(connString);
                conn.Open();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }

            string sql = null;
            if (query)
            {
                sql = spalten[0];
            }
            else
            {
                sql = "SELECT ";
                for (int i = 0; i < (spalten.Length - 1); i++)
                {
                    sql += (spalten[i] + ", ");
                }
                sql += (spalten[spalten.Length - 1]);
                sql += (" FROM " + Tablename);
                sql += filter;
            }
            Console.WriteLine("Query: "+sql);

            MySqlCommand command = conn.CreateCommand();
            command.CommandText = sql;
            int result = command.ExecuteNonQuery();

            MySqlDataAdapter daDataAdapterMySql = new MySql.Data.MySqlClient.MySqlDataAdapter(sql, conn);
            DataTable data = new DataTable();
            try
            {
                daDataAdapterMySql.Fill(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }

            return data;
        }

        private void getMySqlTables(string mySqlDbName)
        {
            string filterString = " WHERE deleted=0";
            string[] spalten = new string[] { "ID", "Code", "Ifc_Name", "Name", "Description", "Parent_ID" };
            DataTable Components = getTable(mySqlDbName, "components", spalten, false, filterString);

            spalten = new string[] { "ID", "Code", "Ifc_Name", "Name", "Description", "Parent_ID" };
            DataTable Material = getTable(mySqlDbName, "material", spalten, false, filterString);

            spalten = new string[] { "ID", "Code", "Description" };
            DataTable Phases        = getTable(mySqlDbName, "phases", spalten, false, "");
            
            spalten = new string[] { "ID", "Code", "Name", "Description", "Name_Int", "Descr_Int", "bIsInbSDD"};
            DataTable Parameters = getTable(mySqlDbName, "parameters", spalten, false, filterString);
            
            spalten = new string[] { "SELECT value_list_defs.ID, value_list_defs.Name, unit_types.Comment FROM value_list_defs LEFT JOIN unit_types ON value_list_defs.Unit_ID = unit_types.ID" };
            DataTable Measures = getTable(mySqlDbName, null, spalten, true, "");

            spalten = new string[] { "ID", "Description", "Value" };
            DataTable Values = getTable(mySqlDbName, "value_lists", spalten, false, filterString);

            filterString = " WHERE Parameters.deleted=0";
            spalten = new string[] { "SELECT Components.ID AS 'Component', Parameters.ID AS 'Parameter', Parameters.Phase_ID AS 'PhaseID' FROM Components INNER JOIN Parameters ON Parameters.Object_ID = Components.ID " + filterString + " ORDER BY Components.ID" };
            DataTable Assigns_Properties = getTable(mySqlDbName, null, spalten, true, filterString);

            spalten = new string[] { "SELECT Material.ID AS 'Material', Parameters.ID AS 'Parameter', Parameters.Phase_ID AS 'PhaseID' FROM Material INNER JOIN Parameters ON Parameters.material = Material.ID" + filterString + " ORDER BY Material.ID" };
            DataTable Assigns_Properties_M = getTable(mySqlDbName, null, spalten, true, "deleted=0");

            spalten = new string[] { "SELECT parameters.ID AS Property, value_list_defs.ID AS Measure FROM parameters LEFT JOIN value_list_defs ON parameters.Value_List_Defs_ID = value_list_defs.ID" + filterString + " ORDER BY parameters.ID" };
            DataTable Assigns_Measures = getTable(mySqlDbName, null, spalten, true, "deleted=0");

            filterString = " WHERE value_lists.deleted=0";
            spalten = new string[] { "SELECT value_list_defs.ID AS Measure, value_lists.ID AS Value FROM value_list_defs LEFT JOIN value_lists ON value_list_defs.ID = value_lists.Value_List_Defs_ID" + filterString + " ORDER BY value_list_defs.ID" };
            DataTable Assigns_Values = getTable(mySqlDbName, null, spalten, true, "deleted=0");

            // fill metaData
            foreach (DataRow row in Components.Rows)
            {
                DataRow newRow = metaData.Tables["Subjects"].NewRow();
                newRow["freeBIM_Guid"] = "E"+row["ID"];
                newRow["ShortName"] = row["Code"];
                newRow["Ifc_Name"] = row["Ifc_Name"];
                newRow["Name_Loc"] = row["Name"];
                newRow["Description_Loc"] = row["Description"];
                if (row["Parent_ID"].ToString().Length > 0)
                    newRow["Parent_Guid"] = "E" + row["Parent_ID"];
                else
                    newRow["Parent_Guid"] = row["Parent_ID"]; ;
                newRow["IsMat"] = false;

                metaData.Tables["Subjects"].Rows.Add(newRow);
            }
            foreach (DataRow row in Material.Rows)
            {
                DataRow newRow = metaData.Tables["Subjects"].NewRow();
                newRow["freeBIM_Guid"] = "M" + row["ID"];
                newRow["ShortName"] = row["Code"];
                newRow["Ifc_Name"] = row["Ifc_Name"];
                newRow["Name_Loc"] = row["Name"];
                newRow["Description_Loc"] = row["Description"];
                if (row["Parent_ID"].ToString().Length > 0)
                    newRow["Parent_Guid"] = "M" + row["Parent_ID"];
                else
                    newRow["Parent_Guid"] = row["Parent_ID"];
                newRow["IsMat"] = true;

                metaData.Tables["Subjects"].Rows.Add(newRow);
            }
            foreach (DataRow row in Phases.Rows)
            {
                DataRow newRow = metaData.Tables["Phases"].NewRow();
                newRow["freeBIM_Guid"] = row["ID"];
                newRow["ShortName"] = row["Code"];
                newRow["Description_Loc"] = row["Description"];

                metaData.Tables["Phases"].Rows.Add(newRow);
            }
            foreach (DataRow row in Parameters.Rows)
            {
                DataRow newRow = metaData.Tables["Properties"].NewRow();
                newRow["freeBIM_Guid"] = row["ID"];
                newRow["ShortName"] = row["Code"];
                newRow["Name_Loc"] = row["Name"];
                newRow["Description_Loc"] = row["Description"];

                metaData.Tables["Properties"].Rows.Add(newRow);
            }
            foreach (DataRow row in Measures.Rows)
            {
                DataRow newRow = metaData.Tables["Measures"].NewRow();
                newRow["freeBIM_Guid"] = row["ID"];
                newRow["Name_Loc"] = row["Name"].ToString().Replace(':', '_');

                metaData.Tables["Measures"].Rows.Add(newRow);
            }
            foreach (DataRow row in Values.Rows)
            {
                DataRow newRow = metaData.Tables["Values"].NewRow();
                newRow["freeBIM_Guid"] = row["ID"];
                newRow["Name_Loc"] = row["Value"];
                newRow["Description_Loc"] = row["Description"];

                metaData.Tables["Values"].Rows.Add(newRow);
            }
            foreach (DataRow row in Assigns_Properties.Rows)
            {
                DataRow newRow = metaData.Tables["Assigns_Properties"].NewRow();
                newRow["Guid_Subject"] = "E"+row["Component"];
                newRow["Guid_Property"] = row["Parameter"];
                newRow["Guid_Phase"] = row["PhaseID"];

                metaData.Tables["Assigns_Properties"].Rows.Add(newRow);
            }
            foreach (DataRow row in Assigns_Properties_M.Rows)
            {
                DataRow newRow = metaData.Tables["Assigns_Properties"].NewRow();
                newRow["Guid_Subject"] = "M" + row["Material"];
                newRow["Guid_Property"] = row["Parameter"];
                newRow["Guid_Phase"] = row["PhaseID"];

                metaData.Tables["Assigns_Properties"].Rows.Add(newRow);
            }
            foreach (DataRow row in Assigns_Measures.Rows)
            {
                DataRow newRow = metaData.Tables["Assigns_Measures"].NewRow();
                newRow["Guid_Property"] = row["Property"];
                newRow["Guid_Measure"] = row["Measure"];

                metaData.Tables["Assigns_Measures"].Rows.Add(newRow);
            }
            foreach (DataRow row in Assigns_Values.Rows)
            {
                DataRow newRow = metaData.Tables["Assigns_Values"].NewRow();
                newRow["Guid_Measure"] = row["Measure"];
                newRow["Guid_Value"] = row["Value"];

                metaData.Tables["Assigns_Values"].Rows.Add(newRow);
            }

            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string[] spalten = new string[] { "ID", "category_integer", "name_deu" };
            DataTable rvt_categories = getTable("revitmapping", "rvt_categories", spalten, false, "");
            Rvt_Categories categories = new Rvt_Categories();
            categories.Tables.Add(rvt_categories);
            categories.WriteXml(@"C:\Users\user\Documents\GitHub\Revit_Mapping\data\Rvt_Categories.xml");
        }
        private string getDefaultBrowser()
        {
            string browser = string.Empty;
            RegistryKey key = null;
            try
            {
                key = Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command", false);

                //trim off quotes
                browser = key.GetValue(null).ToString().ToLower().Replace("\"", "");
                if (!browser.EndsWith("exe"))
                {
                    //get rid of everything after the ".exe"
                    browser = browser.Substring(0, browser.LastIndexOf(".exe") + 4);
                }
            }
            finally
            {
                if (key != null) key.Close();
            }
            return browser;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string browserExe = getDefaultBrowser();
            Process browser= new Process();
            browser.EnableRaisingEvents = true;
            browser.StartInfo.Arguments = textBox1.Text;
            browser.StartInfo.FileName = browserExe;
            browser.Start();
        }


    }
}
