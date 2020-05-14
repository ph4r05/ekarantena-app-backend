﻿using System;
using System.IO;
using System.Linq;
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
using Sygic.Corona.Application.Queries;
using Sygic.Corona.Application.Validations;
using Sygic.Corona.Contracts.Requests;
using Sygic.Corona.Contracts.Responses;
using Sygic.Corona.Domain.Common;
using Sygic.Corona.Infrastructure.Services.Authorization;

namespace Sygic.Corona.QuarantineApi
{
    public class StoreNonce
    {
        private readonly ISignVerification verification;
        private readonly IMediator mediator;
        private readonly ValidationProcessor validation;

        public StoreNonce(ISignVerification verification, IMediator mediator, ValidationProcessor validation)
        {
            this.verification = verification;
            this.mediator = mediator;
            this.validation = validation;
        }

        [FunctionName("StoreNonce")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "nonce")] HttpRequest req,
            ILogger log, CancellationToken cancellationToken)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                string[] signatureHeaderParameters = req.Headers["X-Signature"].ToString().Split(':');
                if (signatureHeaderParameters.Length != 2)
                {
                    return new BadRequestResult();
                }
                var isVerified = verification.Verify(requestBody, signatureHeaderParameters.First(), signatureHeaderParameters.Last());

                if (!isVerified)
                {
                    return new UnauthorizedResult();
                }

                var data = JsonConvert.DeserializeObject<StoreNonceRequest>(requestBody);
                var command = new StoreNonceCommand(data.CovidPass);
                
                await mediator.Send(command, cancellationToken);
                
                var query = new RetrieveNonceQuery(data.CovidPass);
                var nonceEntry = await mediator.Send(query, cancellationToken);

                return new OkObjectResult(new StoreNonceResponse
                {
                    Nonce = nonceEntry.Nonce
                });
            }
            catch (DomainException ex)
            {
                var errors = validation.ProcessErrors(ex);
                return new BadRequestObjectResult(errors);
            }
        }
    }
}