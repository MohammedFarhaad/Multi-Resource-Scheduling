using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fhirapis
{
    public class Appointmentrecord
    {
        //test
        public string resourceType { get; set; }
        public appointmentType appointmentType { get; set; }
        public List<identifiervalue> identifier { get; set; }
        public List<reasonCodes> reasonCode { get; set; }
        public string status { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public int minutesDuration { get; set; }
        public string created { get; set; }
        public List<object> participant { get; set; }

    }
    public class elseAppointmentrecord
    {
        public string resourceType { get; set; }
        public appointmentType appointmentType { get; set; }
        public List<identifiervalue> identifier { get; set; }
        public List<reasonCodes> reasonCode { get; set; }
        public string status { get; set; }
        public string description { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public int minutesDuration { get; set; }
        public string created { get; set; }
        public List<object> participant { get; set; }

    }

    public class appointmentType
    {
        public string text { get; set; }
    }
    public class identifiervalue
    {
        public string value { get; set; }
    }
    public class reasonCodes
    {
        public string text { get; set; }
    }
    public class participants
    {
        public List<types> type { get; set; }
        public actor actor { get; set; }
        public string required { get; set; }
        public string status { get; set; }
    }
    public class types
    {
        public string text { get; set; }
    }
    public class actor
    {
        public string reference { get; set; }
        public string type { get; set; }
    }
    
    public class AppointmentEMR : IPlugin
    {
        ITracingService tracingService;
        IOrganizationService database;
        IPluginExecutionContext context;

        public string Appointmentidvalue;
        public string AppointmentType;
        public string ReasonForAppointment;
        public string Discription;
        public string StartTime;
        public string EndTime;
        public string Createdon;
        public int Duration;
        public object participantrecord;
        public object SurgeryID;
        private object appointment;
        private Guid AppointmentEMR_ID;
        List<object> records = new List<object>();
        List<string> participantsnameslist = new List<string>();
        List<string> participantsactortypeslist = new List<string>();
        List<string> participantsrequiredlist = new List<string>();
        List<string> participantsstatuslist = new List<string>();

        public async void Execute(IServiceProvider serviceProvider)
        {
            init(serviceProvider);
            tracingService.Trace("Plugin connection is established");

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                tracingService.Trace("in if statement");

                // Target entity Appointment_EMR_Participants
                Entity entity = (Entity)context.InputParameters["Target"];
                Entity Appointment_Participants = entity;

                tracingService.Trace("Entity name = " + Appointment_Participants.LogicalName);

                // Record ID
                Guid ParticipantsID = Appointment_Participants.Id;
                Appointmentidvalue = Convert.ToString(ParticipantsID);
                tracingService.Trace(ParticipantsID.ToString());

                // retrieving Appointment_EMR_Participant record attributes
                Entity allRecords = database.Retrieve("msemr_appointmentemrparticipant", ParticipantsID, new ColumnSet(true));
                object AppointmentID;
                if (allRecords.Attributes.Contains("msemr_appointmentemr"))
                {
                    string AppointemntEMR = ((EntityReference)allRecords.Attributes["msemr_appointmentemr"]).Name;
                    AppointmentEMR_ID = ((EntityReference)allRecords.Attributes["msemr_appointmentemr"]).Id;
                    AppointmentID = AppointmentEMR_ID;
                }
                else
                {
                    AppointmentID = null;
                }
                tracingService.Trace("Appointment emr id= "+ AppointmentID);

                if (AppointmentID != null)
                {
                    //collecting Appointment EMR record details
                    Entity Appointment_emr = new Entity("msemr_appointmentemr");
                    Entity Appointment_row = database.Retrieve("msemr_appointmentemr", AppointmentEMR_ID, new ColumnSet(true));
                    
                    string entityname = Appointment_row.LogicalName;
                    tracingService.Trace(entityname);

                    AppointmentType = ((EntityReference)Appointment_row.Attributes["msemr_appointmenttype"]).Name;
                    if (Appointment_row.Attributes.Contains("cred0_reasonforappointment"))
                    {
                        ReasonForAppointment = ((EntityReference)Appointment_row.Attributes["cred0_reasonforappointment"]).Name;
                        Guid SurgeryTypeID = ((EntityReference)Appointment_row.Attributes["cred0_reasonforappointment"]).Id;
                        SurgeryID = SurgeryTypeID;
                    }
                    else
                    {
                        ReasonForAppointment = null;
                        SurgeryID = null;
                    }
                    if (Appointment_row.Attributes.Contains("msemr_description"))
                    {
                        Discription = Convert.ToString(Appointment_row.Attributes["msemr_description"]);
                    }
                    else
                    {
                        Discription = null;
                    }
                    Duration = Convert.ToInt32(Appointment_row.Attributes["new_duration"]);

                    DateTime startTime = Convert.ToDateTime(Appointment_row.Attributes["msemr_starttime"]);
                    StartTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss" + "Z");

                    DateTime endTime = Convert.ToDateTime(Appointment_row.Attributes["msemr_endtime"]);
                    EndTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss" + "Z");

                    DateTime createdon = Convert.ToDateTime(Appointment_row.Attributes["createdon"]);
                    Createdon = createdon.ToString("yyyy-MM-ddTHH:mm:ss" + "Z");

                    tracingService.Trace(Createdon + "," + EndTime + "," + StartTime + "," + Duration + "," + AppointmentType + "," + ReasonForAppointment);
                }

                //collect records of Appointment EMR Participants
                Entity AppointmentParticipants = new Entity("msemr_appointmentemrparticipant");
                tracingService.Trace(AppointmentParticipants.LogicalName);
                    
                //Querying Participants
                QueryExpression queryparticipants = new QueryExpression("msemr_appointmentemrparticipant");
                queryparticipants.ColumnSet = new ColumnSet(true);

                if (SurgeryID != null && AppointmentID != null)
                {                    
                    FilterExpression filterparticipants = new FilterExpression(LogicalOperator.And);
                    filterparticipants.AddCondition("msemr_appointmentemr", ConditionOperator.Equal, AppointmentID);
                    queryparticipants.Criteria = filterparticipants;
                    EntityCollection Participants = database.RetrieveMultiple(queryparticipants);
                    int participants_count = 0;
                    Participants.Entities.ToList().ForEach(details =>
                    {
                        string participantname = Convert.ToString(details.Attributes["msemr_name"]);
                        string participantactortype = details.FormattedValues["msemr_participantactortype"].ToString();
                        if(participantactortype == "Practitioner")
                        {
                            //participantname = participantname.Split(' ')[0];
                            Guid contactid = ((EntityReference)details.Attributes["msemr_actorphysician"]).Id;
                            object physicianID = contactid;

                            Entity contact = new Entity("contact");
                            Entity contact_row = database.Retrieve("contact", contactid, new ColumnSet(true));

                            string email = Convert.ToString(contact_row.Attributes["emailaddress1"]);
                            participantname = email;
                        }
                        string participantrequired = details.FormattedValues["msemr_required"].ToString().ToLower();
                        string participantstatus = details.FormattedValues["msemr_participationstatus"].ToString().ToLower();
                        participantsnameslist.Add(participantname);
                        participantsactortypeslist.Add(participantactortype);
                        participantsrequiredlist.Add(participantrequired);
                        participantsstatuslist.Add(participantstatus);
                        participants_count++;
                    });

                    //Query Required resource
                    QueryExpression queryresource_required = new QueryExpression("cred0_surgeryresourcerequirement");
                    queryresource_required.ColumnSet = new ColumnSet(true);

                    FilterExpression filter_resourcerequired = new FilterExpression(LogicalOperator.And);

                    FilterExpression filter1 = new FilterExpression(LogicalOperator.And);
                    filter1.AddCondition("cred0_surgery", ConditionOperator.Equal, SurgeryID);

                    FilterExpression filter2 = new FilterExpression(LogicalOperator.Or);
                    filter2.AddCondition("cred0_resourcetype", ConditionOperator.Equal, 297320006);
                    filter2.AddCondition("cred0_resourcetype", ConditionOperator.Equal, 100000001);
                    filter2.AddCondition("cred0_resourcetype", ConditionOperator.Equal, 100000000);

                    filter_resourcerequired.AddFilter(filter1);
                    filter_resourcerequired.AddFilter(filter2);

                    queryresource_required.Criteria = filter_resourcerequired;
                    EntityCollection resources = database.RetrieveMultiple(queryresource_required);
                    int resource_count = 0;
                    resources.Entities.ToList().ForEach(required_resource =>
                    {
                        string resource_names = Convert.ToString(required_resource.Attributes["cred0_resourcename"]);
                        tracingService.Trace(resource_names);
                        resource_count++;
                    });

                    tracingService.Trace(participants_count+","+ resource_count);

                    if(participants_count == resource_count)
                    {
                        //Create participants Json object
                        for (int i = 0; i < participantsnameslist.Count; i++)
                        {
                            if (participantsactortypeslist[i] == "Practitioner" || participantsactortypeslist[i] == "Patient")
                            {
                                participantrecord = new participants
                                {
                                    type = new List<types>
                                {
                                new types
                                {
                                    text = participantsactortypeslist[i]
                                }
                                },
                                    actor = new actor
                                    {
                                        reference = participantsactortypeslist[i] + "?email=" + participantsnameslist[i],
                                        type = participantsactortypeslist[i]
                                    },
                                    required = participantsrequiredlist[i],
                                    status = participantsstatuslist[i]
                                };
                            }
                            if (participantsactortypeslist[i] == "Device")
                            {
                                participantrecord = new participants
                                {
                                    type = new List<types>
                            {
                                new types
                                {
                                    text = participantsactortypeslist[i]
                                }
                            },
                                    actor = new actor
                                    {
                                        reference = participantsactortypeslist[i] + "?device-name=" + participantsnameslist[i],
                                        type = participantsactortypeslist[i]
                                    },
                                    required = participantsrequiredlist[i],
                                    status = participantsstatuslist[i]
                                };
                            }
                            if (participantsactortypeslist[i] == "Location")
                            {
                                participantrecord = new participants
                                {
                                    type = new List<types>
                                {
                                new types
                                {
                                    text = participantsactortypeslist[i]
                                }
                                },
                                    actor = new actor
                                    {
                                        reference = participantsactortypeslist[i] + "?name=" + participantsnameslist[i],
                                        type = participantsactortypeslist[i]
                                    },
                                    required = participantsrequiredlist[i],
                                    status = participantsstatuslist[i]
                                };
                            }
                            var Jserilizedobject = JsonConvert.SerializeObject(participantrecord);
                            var jdeserilizedobject = JsonConvert.DeserializeObject(Jserilizedobject);
                            records.Add(jdeserilizedobject);
                        }

                        //Create Appointemnt json resource
                        if (Discription == null)
                        {
                            appointment = new Appointmentrecord
                            {
                                resourceType = "Appointment",
                                identifier = new List<identifiervalue>
                                {
                                    new identifiervalue
                                    {
                                        value = Appointmentidvalue
                                    }
                                },
                                appointmentType = new appointmentType
                                {
                                    text = AppointmentType
                                },
                                reasonCode = new List<reasonCodes>
                            {
                            new reasonCodes
                            {
                                text = ReasonForAppointment
                            }
                            },
                                status = "booked",
                                start = StartTime,
                                end = EndTime,
                                minutesDuration = Duration,
                                created = Createdon,
                                participant = new List<object>(records)
                            };
                        }
                        else
                        {
                            appointment = new elseAppointmentrecord
                            {
                                resourceType = "Appointment",
                                identifier = new List<identifiervalue>
                                {
                                    new identifiervalue
                                    {
                                        value = Appointmentidvalue
                                    }
                                },
                                appointmentType = new appointmentType
                                {
                                    text = AppointmentType
                                },
                                reasonCode = new List<reasonCodes>
                            {
                            new reasonCodes
                            {
                                text = ReasonForAppointment
                            }
                            },
                                status = "booked",
                                description = Discription,
                                start = StartTime,
                                end = EndTime,
                                minutesDuration = Duration,
                                created = Createdon,
                                participant = new List<object>(records)
                            };
                        }

                        var Createdobj = JsonConvert.SerializeObject(appointment);
                        tracingService.Trace(Createdobj);

                        // calling method to post resourcee in FHIR
                        await PostAppointment(Createdobj);
                    }
                }
            }
        }

        public async Task PostAppointment(object Storeresource)
        {

            var pairs = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>( "grant_type", "password" ),

                        new KeyValuePair<string, string>( "resource", "https://mobicure.azurehealthcareapis.com" ),

                        new KeyValuePair<string, string> ( "Accept", "application/json" ),
                        
                        new KeyValuePair<string, string> ( "Content-type", "application/json" ),
                        
                        new KeyValuePair<string, string> ( "client_id", "5770ff89-11fc-41a5-b99c-c9ff122e0f96" ),
                        
                        new KeyValuePair<string, string> ( "client_secret", "c-FpFiXyzlygj3t709H-0s47hjsY-7_37d" ),
                        
                        new KeyValuePair<string, string>( "username", "farhaad.mohammed@popcornapps.com" ),
                        
                        new KeyValuePair<string, string> ( "Password", "allaha786*" ),
                    };

            var content = new FormUrlEncodedContent(pairs);

            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            string GeneratedToken = "";

            using (var client = new HttpClient())
            {
                var response = client.PostAsync("https://login.microsoftonline.com/f8300747-02c3-470c-a3d6-5a3355e3d77d/oauth2/token", content).Result;

                GeneratedToken = response.Content.ReadAsStringAsync().Result;
            }

            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            using (var client = new HttpClient())
            {
                if (!string.IsNullOrWhiteSpace(GeneratedToken))
                {
                    dynamic t = JsonConvert.DeserializeObject(GeneratedToken);

                    client.DefaultRequestHeaders.Clear();

                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + t.access_token);

                }

                var jsonobj = Storeresource;

                //post resource
                HttpRequestMessage createHttpRequest1 = new HttpRequestMessage(HttpMethod.Post, "https://mobicure.azurehealthcareapis.com/Appointment")
                {
                    Content = new StringContent(jsonobj.ToString(), Encoding.UTF8, "application/json")
                };
                HttpResponseMessage response2 = await client.SendAsync(createHttpRequest1);

                //Get response
                HttpResponseMessage getresource = await client.GetAsync("https://mobicure.azurehealthcareapis.com/Appointment");
                var pageContent = await getresource.Content.ReadAsStringAsync();
                JObject elements = (JObject)JsonConvert.DeserializeObject(pageContent);

            }
        }
        private void init(IServiceProvider serviceProvider)
        {
            tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            database = serviceFactory.CreateOrganizationService(context.UserId);
        }
             
    }
}
