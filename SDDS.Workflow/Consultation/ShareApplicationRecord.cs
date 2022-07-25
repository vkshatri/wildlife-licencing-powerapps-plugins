using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace SDDS.Workflow.Consultation
{
    public class ShareApplicationRecord : CodeActivity
    {
        [RequiredArgument]
        [Input("ApplicationRef")]
        [ReferenceTarget("sdds_application")]
        public InArgument<EntityReference> ApplicationRef { get; set; }

        [Input("TeamRef")]
        [ReferenceTarget("team")]
        public InArgument<EntityReference> TeamRef { get; set; }


        protected override void Execute(CodeActivityContext executionContext)
        {
            //Create the context
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = (ITracingService)executionContext.GetExtension<ITracingService>();
            tracingService.Trace("Entering workflow");
            try
            {
                if (context.InputParameters.Contains("Target"))
                {
                    Entity target = (Entity)context.InputParameters["Target"];
                    if (target == null) return;
                    switch (target.LogicalName)
                    {
                        case "sdds_application":
                            ShareUnshareApplicationToAdvisor(target, context, service, tracingService);
                            break;
                        case "sdds_consultation":
                            ShareApplicationRecordToConsulting(executionContext, service);
                            break;

                    }

                }

            }
            catch (Exception ex)
            {
                tracingService.Trace("Failure in custom workflow action: {0}", ex.Message);

                throw new InvalidWorkflowException("Failure in custom workflow action: " + ex.Message);
            }
        }

        /// <summary>
        /// Share the application record to the consulting team.
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="service"></param>
        private void ShareApplicationRecordToConsulting(CodeActivityContext executionContext, IOrganizationService service)
        {
            EntityReference application = ApplicationRef.Get<EntityReference>(executionContext);
            if (application == null || application.LogicalName != "sdds_application") return;

            EntityReference team = TeamRef.Get<EntityReference>(executionContext);
            if (team == null || team.LogicalName != "team") return;

            // Grant the team read/write access to the lead.
            ShareRecord(application, team, service);

        }

        /// <summary>
        /// Shares/UnShare the application record with Advisor/Lead Advisor.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="context"></param>
        /// <param name="service"></param>
        private void ShareUnshareApplicationToAdvisor(Entity target, IWorkflowContext context,
            IOrganizationService service, ITracingService tracingService)
        {
            tracingService.Trace("Entering ShareUnshareApplicationToAdvisor");
            EntityReference advisor = null;
            EntityReference oldAdvisor = null;
            EntityReference oldLeadAdvisor = null;
            EntityReference leadAdvisor = null;

            if (target.Attributes.Contains("sdds_assessorid"))
            {
                advisor = target.GetAttributeValue<EntityReference>("sdds_assessorid");
                ShareRecord(target.ToEntityReference(), advisor, service);

            }
            if (target.Attributes.Contains("sdds_leadadvisor"))
            {
                leadAdvisor = target.GetAttributeValue<EntityReference>("sdds_leadadvisor");
                ShareRecord(target.ToEntityReference(), leadAdvisor, service);

            }
            if (context.PreEntityImages.ContainsKey("PreBusinessEntity"))
            {
                Entity preImage = context.PreEntityImages["PreBusinessEntity"];
                if (preImage != null && preImage.LogicalName == "sdds_application")
                {
                    oldAdvisor = preImage.GetAttributeValue<EntityReference>("sdds_assessorid");
                    oldLeadAdvisor = preImage.GetAttributeValue<EntityReference>("sdds_leadadvisor");

                }
                if (advisor != null && oldAdvisor != null)
                {
                    if (advisor.Id != oldAdvisor.Id)
                        UnShareRecord(target.ToEntityReference(), oldAdvisor, service);

                }
                if (leadAdvisor != null && oldLeadAdvisor != null)
                {
                    if (leadAdvisor.Id != oldLeadAdvisor.Id)
                        UnShareRecord(target.ToEntityReference(), oldLeadAdvisor, service);

                }
            }
        }

        /// <summary>
        /// Share the record to user or team.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="teamOrUser"></param>
        /// <param name="service"></param>
        private void ShareRecord(EntityReference application, EntityReference teamOrUser, IOrganizationService service)
        {
            var grantAccessRequest = new GrantAccessRequest
            {
                PrincipalAccess = new PrincipalAccess
                {
                    AccessMask = AccessRights.ReadAccess | AccessRights.WriteAccess,
                    Principal = teamOrUser
                },
                Target = application
            };

            service.Execute(grantAccessRequest);
        }

        /// <summary>
        /// Unshare record from user or team.
        /// </summary>
        /// <param name="application"></param>
        /// <param name="teamOrUser"></param>
        /// <param name="service"></param>
        private void UnShareRecord(EntityReference application, EntityReference teamOrUser, IOrganizationService service)
        {
            // Revoke access to the lead for the second user.
            var revokeUser2AccessReq = new RevokeAccessRequest
            {
                Revokee = teamOrUser,
                Target = application
            };

            service.Execute(revokeUser2AccessReq);
        }
    }
}
