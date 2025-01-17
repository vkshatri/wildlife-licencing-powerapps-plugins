﻿using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDDS.Plugin.ApplicationPriority
{
    public class ApplicationSetPriority : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);
            ITracingService tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            if (context.MessageName.ToLower() != "create") return;

            Guid badger = new Guid("9d62e5b8-9c77-ec11-8d21-000d3a87431b");
            Guid eps = new Guid("05e8951c-f452-ec11-8f8e-000d3a0ce458");

            Entity licenseApp = (Entity)context.InputParameters["Target"];

            var entityId = licenseApp.Id;

            Guid licenseTypeId = licenseApp.GetAttributeValue<EntityReference>("sdds_applicationtypesid").Id;

            tracing.Trace("Entering Plugin");
            try
            {
                AssignPriorityLogic logic = new AssignPriorityLogic();

                if (licenseTypeId == badger || licenseTypeId == eps)
                {
                    if (logic.GetPurpose(licenseApp))
                    {
                        UpdateEntity(licenseApp, (int)ApplicationEnum.Priority.one, service);
                    }else if (logic.CheckBadgerBeaverSpecie(service, entityId) == "beaver" && licenseTypeId == badger)
                    {
                        UpdateEntity(licenseApp, (int)ApplicationEnum.Priority.one, service);
                    }else if (logic.CheckBuzzardRavenRedKiteSpecie(service, entityId) && licenseTypeId == badger)
                    {
                        tracing.Trace("Entering CheckBuzzardRavenRedKiteSpecie");
                        UpdateEntity(licenseApp, (int)ApplicationEnum.Priority.one, service);
                    }else if (logic.CheckBadgerBeaverSpecie(service,entityId) == "badger" && logic.CheckSettTypeAndMethod(service, entityId, tracing))
                    {
                        tracing.Trace("Entering CheckBadgerBeaverSpecie 2");
                        UpdateEntity(licenseApp, (int)ApplicationEnum.Priority.one, service);
                    }else if (logic.UniquenessCheck(service, entityId, tracing))
                    {
                        tracing.Trace("Entering UniquenessCheck");
                        UpdateEntity(licenseApp, (int)ApplicationEnum.Priority.two, service);
                    }else if (logic.MultiPlots(service, entityId,tracing))
                    {
                        tracing.Trace("Entering MultiPlots");
                        UpdateEntity(licenseApp, (int)ApplicationEnum.Priority.two, service);
                    }else if (logic.SeasonalCheck(licenseApp))
                    {
                        tracing.Trace("Entering SeasonalCheck");
                        UpdateEntity(licenseApp, (int)ApplicationEnum.Priority.two, service);
                    }else if (logic.DASSPSS(licenseApp))
                    {
                        tracing.Trace("Entering DASSPSS");
                        UpdateEntity(licenseApp, (int)ApplicationEnum.Priority.two, service);
                    }
                    else { UpdateEntity(licenseApp, (int)ApplicationEnum.Priority.four, service); }
                }
                else { UpdateEntity(licenseApp, (int)ApplicationEnum.Priority.four, service); }
            }
            catch (Exception ex)
            {
                tracing.Trace(ex.Message);
                throw new InvalidPluginExecutionException(ex.Message);

            }

        }
        public static void UpdateEntity(Entity entity, int value, IOrganizationService service)
        {
            service.Update(new Entity(entity.LogicalName, entity.Id)
            {
                ["sdds_priority"] = new OptionSetValue(value)
            }); 
        }
    }
}
