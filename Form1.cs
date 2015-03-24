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
        DataTable Subjects, Properties, Assigns_Properties, Assigns_Measures, prop_par_mapping, Measures, parameters, freeBimWebserviceTable, localFreeBimDataTable;
        string xmlPath;
        Microsoft.Win32.RegistryKey regKey;
        string regKeyName = @"Software\CAD Anwendungen Muigg\Revit_Mapping";
        string metaDataName, mappingDataName;
        string server, user, password, dbName;

        FreebimWebserviceEndpointService service = new FreebimWebserviceEndpointService();
        String dbuser = "manuel.gasteiger";
        String dbpw = "59wnnV&3?e";
        List<string> guidList = new List<string>();
        Dictionary<string, orderedRel[]> childsOf = new Dictionary<string,orderedRel[]>();
        Dictionary<string, string> freebimName = new Dictionary<string, string>();

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
                mappingDataName = xmlPath + "\\parameterMappingXML.xml";
                server= regKey.GetValue("Server").ToString();
                user= regKey.GetValue("User").ToString();
                password= regKey.GetValue("Password").ToString();
                dbName = regKey.GetValue("Database").ToString();
            }
        }

        private void getAllChildsOf(orderedRel c)
        {
            orderedRel[] childs = service.getChildsOf(dbuser, dbpw, c.freebimId);
            if (childs != null)
            {
                int length = childs.Length;
                guidList.Add(c.freebimId);
                childsOf.Add(c.freebimId, childs);

                for (var i = 0; i < length; i++)
                {
                    var child = childs[i];
                    getAllChildsOf(child);
                }
            }
        }

        // load freebim data form webservice and store into XML
        private void button2_Click(object sender, EventArgs e)
        {

            library[] libraries = service.getAllLibraries(dbuser, dbpw);
            foreach (library l in libraries)
            {
                if (l.name == "freeBIM")
                {
                    orderedRel[] root = service.getChildsOf(dbuser, dbpw, l.freebimId);
                    foreach (orderedRel r in root)
                    {
                        getAllChildsOf(r);
                    }
                }
            }

            freeBimWebserviceTable = new DataTable();
            freeBimWebserviceTable.TableName = "allComponents";
            freeBimWebserviceTable.Columns.Add("bsddGuid");
            freeBimWebserviceTable.Columns.Add("Code");
            freeBimWebserviceTable.Columns.Add("desc");
            freeBimWebserviceTable.Columns.Add("freebimId");
            freeBimWebserviceTable.Columns.Add("name");
            freeBimWebserviceTable.Columns.Add("children");
            
            component[] componentList = new component[guidList.Count];
            int i = 0;
            foreach (string s in guidList)
            {
                componentList[i++] = service.getComponent(dbuser, dbpw, s);
            }
            foreach (component c in componentList)
            {
                DataRow row = freeBimWebserviceTable.NewRow();
                row["bsddGuid"] = c.bsddGuid;
                row["code"] = c.code;
                row["desc"] = c.desc;
                row["freebimId"] = c.freebimId;
                row["name"] = c.name;
                row["children"] = orderedRelToString(childsOf[c.freebimId]);

                freeBimWebserviceTable.Rows.Add(row);
                freebimName.Add(c.freebimId,c.name);
            }
            dataGridView7.DataSource = freeBimWebserviceTable;
            freeBimWebserviceTable.WriteXml("freeBIMData.xml");

        }

        private string orderedRelToString(orderedRel[] rel)
        {
            try
            {
                String str = "";
                for (int i = 0; i < rel.Length; i++)
                {
                    str += rel[i].freebimId + "; " ;
                }
                return str;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                return null;
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            DataSet localXml = new DataSet();
            localXml.ReadXml("freeBIMLocalData.xml");
            localFreeBimDataTable = localXml.Tables[0];

            dataGridView1.DataSource = localFreeBimDataTable;
            
        }
        private void button3_Click(object sender, EventArgs e)
        {
//          freeBimWebserviceTable.Merge(localFreeBimDataTable);
//          DataTable changes = localFreeBimDataTable.GetChanges();
//          dataGridView8.DataSource = changes;

            var diff = localFreeBimDataTable.AsEnumerable().Except(freeBimWebserviceTable.AsEnumerable(), DataRowComparer.Default);



            for(int i = freeBimWebserviceTable.Rows.Count -1; i>=0; i--)
            {
                for (int j = localFreeBimDataTable.Rows.Count - 1; j >= 0; j--)
                {
                    var array1 = freeBimWebserviceTable.Rows[i].ItemArray;
                    var array2 = localFreeBimDataTable.Rows[j].ItemArray;

                    if (array1.SequenceEqual(array2))
                    {
                        Console.WriteLine("Korrekter Eintrag für {0}", freeBimWebserviceTable.Rows[i]["name"]);
                        freeBimWebserviceTable.Rows.RemoveAt(i);
//                        localFreeBimDataTable.Rows.RemoveAt(j);
                        break;
                    }
                    else if (freeBimWebserviceTable.Rows[i]["freebimId"] == localFreeBimDataTable.Rows[j]["freebimId"])
                    {
                        localFreeBimDataTable.Rows[j]["Code"] = freeBimWebserviceTable.Rows[i]["Code"];
                        localFreeBimDataTable.Rows[j]["desc"] = freeBimWebserviceTable.Rows[i]["desc"];
                        localFreeBimDataTable.Rows[j]["freebimId"] = freeBimWebserviceTable.Rows[i]["freebimId"];
                        localFreeBimDataTable.Rows[j]["name"] = freeBimWebserviceTable.Rows[i]["name"];
                        localFreeBimDataTable.Rows[j]["children"] = freeBimWebserviceTable.Rows[i]["children"];
                        break;
                    }
                }
            }

            localFreeBimDataTable.Rows.Add(diff);
            dataGridView7.DataSource = freeBimWebserviceTable;
            dataGridView8.DataSource = localFreeBimDataTable;
            /*
            foreach (DataRow r in freeBimWebserviceTable.Rows)
            {
                foreach (DataRow s in localFreeBimDataTable.Rows)
                {


                }
                
                DataRow nr = localFreeBimDataTable.Rows.Find(r["freebimId"]);

                if (nr != null)
                {
                    localFreeBimDataTable.Rows.Remove(nr);
                }
                localFreeBimDataTable.Rows.Add(r);
            }

            dataGridView1.DataSource = localFreeBimDataTable;
            */
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

        private void button5_Click(object sender, EventArgs e)
        {
            metaData.Clear();
            metaData.ReadXml(metaDataName);
            rvt_parameter_mapping.Clear();
            rvt_parameter_mapping.ReadXml(mappingDataName);
            Subjects = metaData.Tables["Subjects"];
            Properties = metaData.Tables["Properties"];
            prop_par_mapping = rvt_parameter_mapping.Tables["prop_par_mapping"];
            parameters = rvt_parameter_mapping.Tables["Parameters"];

            int category_int = -2000011;
            // First find the Properties relevant for the subjects, the list is a result from the category and the inheritance chain

            List<string> parents = new List<string> { "E1", "E81" };

            Assigns_Properties = metaData.Tables["Assigns_Properties"];
            DataTable element_props = new DataTable();

            string[] cols2 = { "Property_ID",  "bSDD_Guid",    "Name_Loc",     "Description", "bIsInIFC_PSet"};
            Type[] dataTypes = {typeof(string), typeof(string), typeof(string), typeof(string),typeof(bool)};
            for (int i = 0; i < cols2.Count(); i++)
            {
                element_props.Columns.Add(cols2[i], dataTypes[i]);
            }

            var result = from p in Properties.AsEnumerable()
                         join r in Assigns_Properties.AsEnumerable() on p.Field<string>("freeBIM_Guid") equals r.Field<string>("Guid_Property")
                         where parents.Contains(r.Field<string>("Guid_Subject"))
                         select new
                         {
                             Property_ID = (string)p["freeBIM_Guid"],
                             bSDD_Guid = p["bSDD_Guid"] == null  ? "" : p["bSDD_Guid"].ToString(),
                             Name_Loc = p["Name_Loc"] == null ? "" : p["Name_Loc"].ToString(),
                             bIsInIFC_PSet = p["bIsInIFC_PSet"] == null || p["bIsInIFC_PSet"].ToString().Length == 0 ? false : Convert.ToBoolean(p["bIsInIFC_PSet"]),
                             Description = p["Description_Loc"] == null ? "" : p["Description_Loc"].ToString()
                         };

            foreach (var item in result)
            {
                DataRow newRow = element_props.NewRow();
                newRow["Property_ID"] = item.Property_ID;
                newRow["bsDD_Guid"] = item.bSDD_Guid;
                newRow["Name_Loc"] = item.Name_Loc;
                newRow["bIsInIFC_PSet"] = item.bIsInIFC_PSet;
                newRow["Description"] = item.Description;
                element_props.Rows.Add(newRow);
            }

            var pars_mapped_query = from m in prop_par_mapping.AsEnumerable()
                              join p in Properties.AsEnumerable() on m.Field<string>("Prop_ID") equals p.Field<string>("freeBIM_Guid")
                              join par in parameters.AsEnumerable() on m.Field<Int32>("Parameter_ID") equals par.Field<Int32>("ID")
                              where m.Field<Int32>("Category_int") == category_int
                              select new
                              {
                                   Prop_ID = p["freeBIM_Guid"].ToString(),
                                   ParName = par["name_deu"] == null ? "" : par["name_deu"].ToString(),
                                   Type_Code = par["Type_Code"] == null ? "" : par["Type_Code"].ToString(),
                                   Parameter_ID = m["Parameter_ID"],
                                   bIsType = par["bIsType"] == null || par["bIsType"].ToString().Length == 0 ? false : par["bIsType"]
                              };

            string[] cols = { "Prop_ID",      "ParName",      "bIsType",      "Type_Code",    "Parameter_ID"};
            Type[] types = {   typeof(string), typeof(string), typeof(bool),   typeof(string), typeof(Int32)};

            DataTable pars_mapped= new DataTable();
            for (int i = 0; i < cols.Count(); i++)
                pars_mapped.Columns.Add(cols[i], types[i]);

            foreach (var item in pars_mapped_query)
            {
                DataRow newRow = pars_mapped.NewRow();
                newRow["Prop_ID"] = item.Prop_ID;
                newRow["ParName"] = item.ParName;
                newRow["Type_Code"] = item.Type_Code;
                newRow["Parameter_ID"] = item.Parameter_ID;
                newRow["bIsType"] = item.bIsType;
                pars_mapped.Rows.Add(newRow);
            }

            var query = from prop in element_props.AsEnumerable()
                        join mapped in pars_mapped.AsEnumerable() on prop.Field<string>("Property_ID") equals mapped.Field<string>("Prop_ID") into j
                        from mapped in j.DefaultIfEmpty()
                        select new
                        {
                            Prop_ID= prop.Field<string>("Property_ID"),
                            PropName = prop.Field<string>("Name_Loc") == null ? "" : prop.Field<string>("Name_Loc").ToString(),
                            Description= prop.Field<string>("Description") == null ? "" : prop.Field<string>("Description").ToString(),
                            bSDD_Guid= prop.Field<string>("bSDD_Guid") == null ? "" : prop.Field<string>("bSDD_Guid").ToString(),
                            bIsInIFC_PSet = prop["bIsInIFC_PSet"] == null || prop["bIsInIFC_PSet"].ToString().Length == 0 ? false : Convert.ToBoolean(prop["bIsInIFC_PSet"]),
                            Type_Code = mapped == null ? "" : mapped.Field<string>("Type_Code").ToString(),
                            ParName =  mapped == null ? "" : mapped.Field<string>("ParName").ToString(),
                            bIsType = mapped == null ? false : mapped.Field<bool>("bIsType"),
                            Parameter_ID = mapped == null ? 0 : mapped.Field<Int32>("Parameter_ID")
                        };

            //             sql = "select parameters.ID as ID, Code, Name, parameters.Description as Description, `Order`, data_types.Description as Data_Type, unit_types.Unit as Unit, parameters.GUID as GUID, parameters.bIsType as bIsType,  ";
            //             sql += "bHide, rvt_parameters.Type_Code as Type_Code, rvt_parameters.name_deu as ParName, rvt_Parameter_ID ";

            string[] cols3 = { "Prop_ID",      "PropName",     "Description",  "bIsType",    "Type_Code",    "ParName",       "bIsInIFC_PSet", "bSDD_Guid",     "Parameter_ID" };
            Type[] types3 = {   typeof(string), typeof(string), typeof(string), typeof(bool), typeof(string), typeof(string),  typeof(bool),    typeof(string),  typeof(Int32) };

            DataTable dt_par_element = new DataTable();
            for (int i = 0; i < cols3.Count(); i++)
                dt_par_element.Columns.Add(cols3[i], types3[i]);

            foreach (var item in query)
            {
                DataRow newRow = dt_par_element.NewRow();
                newRow["Prop_ID"] = item.Prop_ID;
                newRow["PropName"] = item.PropName;
                newRow["Description"] = item.Description;
                newRow["bIsType"] = item.bIsType;
                newRow["Type_Code"] = item.Type_Code;
                newRow["ParName"] = item.ParName;
                newRow["bIsInIFC_PSet"] = item.bIsInIFC_PSet;
                newRow["bSDD_Guid"] = item.bSDD_Guid;
                newRow["Parameter_ID"] = item.Parameter_ID;
                dt_par_element.Rows.Add(newRow);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            service = new FreebimWebserviceEndpointService();

            component[] stuff = service.getAllComponents(dbuser, dbpw);
            orderedRel[] param, child;

            DataTable table = new DataTable();
            table.TableName = "allComponents";
            table.Columns.Add("bsddGuid");
            table.Columns.Add("Code");
            table.Columns.Add("desc");
            table.Columns.Add("freebimId");
            table.Columns.Add("name");
            table.Columns.Add("parameters");
            table.Columns.Add("childs");

            foreach (component c in stuff)
            {
                DataRow row = table.NewRow();
                row["bsddGuid"] = c.bsddGuid;
                row["code"] = c.code;
                row["desc"] = c.desc;
                row["freebimId"] = c.freebimId;
                row["name"] = c.name;

                param = service.getParameterOf(dbuser, dbpw, c.freebimId);
                if (param != null)
                {
                    foreach (orderedRel p in param)
                    {
                        row["parameters"] += service.getParameter(dbuser, dbpw, p.freebimId).name + "; ";
                    }
                }

                child = service.getChildsOf(dbuser, dbpw, c.freebimId);
                if (child != null)
                {
                    foreach (orderedRel ch in child)
                    {
                        row["childs"] += service.getComponent(dbuser, dbpw, ch.freebimId).name + "; ";
                    }
                }

                table.Rows.Add(row);
            }

            dataGridView8.DataSource = table;
            table.WriteXml("AllData.xml");
            table.WriteXml("C:\\Users\\user\\Documents\\AllData.xml");
        }

    }
}
