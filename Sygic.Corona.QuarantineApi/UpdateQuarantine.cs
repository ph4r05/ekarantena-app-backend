﻿using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Sygic.Corona.Application.Commands;
using Sygic.Corona.Application.Validations;
using Sygic.Corona.Contracts.Requests;
using Sygic.Corona.Domain;
using Sygic.Corona.Domain.Common;
using System.Web.Http;
using System;
using Microsoft.Extensions.Configuration;

namespace Sygic.Corona.QuarantineApi
{
    public class UpdateQuarantine
    {
        private readonly IMediator mediator;
        private readonly IConfiguration configuration;
        private readonly ValidationProcessor validation;

        public UpdateQuarantine(IMediator mediator, IConfiguration configuration, ValidationProcessor validation)
        {
            this.mediator = mediator;
            this.configuration = configuration;
            this.validation = validation;
        }

        [FunctionName("UpdateQuarantine")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "quarantine")]
            HttpRequest req, ILogger log, CancellationToken cancellationToken)
        {
            try
            {
                var qs = req.Query;

                if (!qs.ContainsKey("apiKey"))
                {
                    return new BadRequestErrorMessageResult("Missing query param: apiKey");
                }

                var apiKey = qs["apiKey"];
                bool isAuthorized = apiKey == Environment.GetEnvironmentVariable("NcziApiKey");

                if (!isAuthorized)
                {
                    log.LogWarning("Unauthorized call.");
                    return new UnauthorizedResult();
                }

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<UpdateQuarantineRequest>(requestBody);
                var command = new UpdateQuarantineCommand(data.CovidPass, data.QuarantineStart, data.QuarantineEnd, data.BorderCrossedAt,
                    new Address(
                        data.QuarantineAddressLatitude, data.QuarantineAddressLongitude,
                        data.QuarantineAddressCountry, data.QuarantineAddressCity, data.QuarantineAddressCityZipCode,
                        data.QuarantineAddressStreetName, data.QuarantineAddressStreetNumber
                    ),
                    configuration["UpdateQuarantineNotificationTitle"], configuration["UpdateQuarantineNotificationBody"]
                );

                await mediator.Send(command, cancellationToken);

                return new OkResult();
            }
            catch (DomainException ex)
            {
                var errors = validation.ProcessErrors(ex);
                return new BadRequestObjectResult(errors);
            }
        }
    }
}