using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using PureCrmGeneratorCmd;

namespace Rappen.XTB.LCG.Cmd
{
    public class LCGHelper
    {
        private List<EntityMetadataProxy> entities;

        private CrmConnection crmConnection;
        private string crmConnectionString;

        public Settings Settings { get; private set; }

        public void ConnectCrm(CrmCredentials credentials)
        {
            this.crmConnection = CrmConnection.ConnectCrm(credentials);
        }

        public void ConnectCrm(string connectionString)
        {
            Console.WriteLine("Connecting Pure Crm...");
            this.crmConnection = CrmConnection.ConnectCrm(connectionString);
            this.crmConnectionString = connectionString;
        }

        public void LoadSettingsFromFile(string path)
        {
            Console.WriteLine("Loading Setting From File.");
            if (File.Exists(path))
            {
                var document = new XmlDocument();
                document.Load(path);
                this.Settings = (Settings)XmlSerializerHelper.Deserialize(document.OuterXml, typeof(Settings));
            }
            else
            {
                throw new FileNotFoundException(path);
            }
        }

        public void GenerateConstants()
        {
            Console.WriteLine("Generating Constants...");
            Settings.InitalizeCommonSettings(); // ToDo: Load CommonSettings
            LoadEntities();
            RestoreSelectedEntities();
            var message = CSharpUtils.GenerateClasses(entities, Settings, Settings.GetWriter(this.crmConnection.WebApplicationUrl));
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.WriteLine("************************* Task End *************************");
            Console.ReadLine();
        }

        private void LoadEntities() // ToDo: Deduplicate with LCG.cs
        {
            var response = MetadataHelper.LoadEntities(this.crmConnection.OrganizationService, this.crmConnection.OrganizationMajorVersion);

            var metaresponse = response.EntityMetadata;
            entities = new List<EntityMetadataProxy>(
                metaresponse
                    .Select(m => new EntityMetadataProxy(m))
                    .OrderBy(e => e.ToString()));
            foreach (var entity in entities)
            {
                entity.Relationships = new List<RelationshipMetadataProxy>(
                    entity.Metadata.ManyToOneRelationships.Select(m => new RelationshipMetadataProxy(entities, m)));
                entity.Relationships.AddRange(
                    entity.Metadata.OneToManyRelationships
                        .Where(r => !entity.Metadata.ManyToOneRelationships.Select(r1m => r1m.SchemaName).Contains(r.SchemaName))
                        .Select(r => new RelationshipMetadataProxy(entities, r)));
            }
        }

        private void LoadAttributes(EntityMetadataProxy entity)  // ToDo: Deduplicate with LCG.cs
        {
            entity.Attributes = null;

            var retreiveMetadataChangeResponse = MetadataHelper.LoadEntityDetails(this.crmConnection.OrganizationService, entity.LogicalName);
            if (retreiveMetadataChangeResponse != null 
                && retreiveMetadataChangeResponse.EntityMetadata != null  
                && retreiveMetadataChangeResponse.EntityMetadata.Count > 0)
            {
                var entityMetadata = retreiveMetadataChangeResponse.EntityMetadata[0];

                entity.Attributes = new List<AttributeMetadataProxy>(
                    retreiveMetadataChangeResponse.EntityMetadata[0]
                        .Attributes
                        .Select(m => new AttributeMetadataProxy(entity, m))
                        .OrderBy(a => a.ToString()));
            }
        }

        private void RestoreSelectedEntities() // ToDo: Deduplicate with LCG.cs
        {
            if (entities == null)
            {
                return;
            }
            if (Settings == null)
            {
                return;
            }
            using (var svc = new CrmServiceClient(crmConnectionString))
            {
                Dictionary<string, string> entityList = new Dictionary<string, string>();

                var entityMetaList = svc.GetAllEntityMetadata(true, Microsoft.Xrm.Sdk.Metadata.EntityFilters.Entity);

                foreach(var em in entityMetaList)
                {
                    entityList.Add(em.LogicalName, em.DisplayName?.UserLocalizedLabel?.Label);
                }

                entityList = entityList
                    .Where(el => !Settings.Selection.Contains(el.Key))
                    .Where(el => !ExludeList.Entities.Contains(el.Key))
                    .ToDictionary(el => el.Key, el => el.Value);

                Console.WriteLine($"Total Entity Count : {entityList.Count}");

                foreach (var e in entityList)
                {
                    if(e.Value == null)
                    {
                        continue;
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Working in {e.Value}");
                    var meta = svc.GetEntityMetadata(e.Key, Microsoft.Xrm.Sdk.Metadata.EntityFilters.All);
                    var attributes = new List<string>();
                    foreach(var attr in meta.Attributes)
                    {
                        if(!ExludeList.Attrs.Contains(attr.DisplayName?.UserLocalizedLabel?.Label?.ToLower()))
                            attributes.Add(attr.LogicalName);
                    }
                    var entity = entities.FirstOrDefault(et => et.LogicalName == e.Key);
                    if (entity == null)
                    {
                        continue;
                    }
                    if (!entity.Selected)
                    {
                        entity.SetSelected(true);
                    }

                    foreach (var attributename in attributes)
                    {
                        if (entity.Attributes == null)
                        {
                            LoadAttributes(entity);
                        }
                        var attribute = entity.Attributes.FirstOrDefault(a => a.LogicalName == attributename);
                        if (attribute != null && !attribute.Selected)
                        {
                            attribute.SetSelected(true);
                        }
                    }
                }
            }
        }
    }
}
