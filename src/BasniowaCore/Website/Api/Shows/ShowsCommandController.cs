﻿using System;
using System.Threading.Tasks;
using AutoMapper;
using Common.Cqrs;
using Common.Startup;
using Logic.Common;
using Logic.Services;
using Logic.Shows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Website.Infrastructure;
using Website.Infrastructure.ErrorHandling;

namespace Website.Api.Shows
{
    /// <summary>
    /// API controller for managing shows.
    /// </summary>
    /// <seealso cref="Controller" />
    [Route("api/shows")]
    [Produces(ContentTypes.ApplicationJson)]
    public class ShowsCommandController : Controller
    {
        private const string GroupName = "Shows";

        /// <summary>
        /// Gets or sets the identifier service.
        /// </summary>
        [InjectService]
        public IUniqueIdService IdService { get; set; }

        /// <summary>
        /// Gets or sets the command sender.
        /// </summary>
        [InjectService]
        public ICommandSender CommandSender { get; set; }

        /// <summary>
        /// Gets or sets the mapper.
        /// </summary>
        [InjectService]
        public IMapper Mapper { get; set; }

        /// <summary>
        /// Creates a new show.
        /// </summary>
        /// <param name="commandModel">Contains information about the show to create.</param>
        /// <returns>ID of created show.</returns>
        /// <response code="200">Information about created show.</response>
        /// <response code="400">When the provided information are invalid.</response>
        [HttpPost]
        [Route("commands/add")]
        [Authorize]
        public async Task<ShowAddedModel> Add([FromBody]AddShowModel commandModel)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpErrorException(BadRequest(ModelState));
            }

            var command = new AddShowCommand();
            Mapper.Map(commandModel, command);

            command.ShowId = await IdService.GenerateId();
            command.UserName = User.Identity.Name;

            await CommandSender.Send(command);

            var result = new ShowAddedModel
            {
                ShowId = command.ShowId
            };

            return result;
        }

        /// <summary>
        /// Updates an existing show.
        /// </summary>
        /// <param name="commandModel">Contains information about the show to update.</param>
        /// <response code="200">Show has been updated.</response>
        /// <response code="400">Request is invalid or doesn't pass validation.</response>
        /// <response code="404">Show of provided ID doesn't exist or has been deleted.</response>
        [HttpPost]
        [Route("commands/update")]
        [Authorize]
        public async Task Update([FromBody]UpdateShowModel commandModel)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpErrorException(BadRequest(ModelState));
            }

            var command = new UpdateShowCommand();
            Mapper.Map(commandModel, command);
            command.UserName = User.Identity.Name;

            try
            {
                await CommandSender.Send(command);
            }
            catch (EntityNotFoundException<DataAccess.Shows.Show>)
            {
                throw new HttpErrorException(NotFound());
            }
        }

        /// <summary>
        /// Marks an existing show as deleted.
        /// </summary>
        /// <param name="commandModel">Contains information about the show to delete.</param>
        /// <response code="200">Show has been deleted.</response>
        /// <response code="400">Request is invalid or doesn't pass validation.</response>
        /// <response code="404">Show of provided ID doesn't exist.</response>
        [HttpPost]
        [Route("commands/delete")]
        [Authorize]
        public async Task Delete([FromBody]DeleteShowModel commandModel)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpErrorException(BadRequest(ModelState));
            }

            var command = new DeleteShowCommand();
            Mapper.Map(commandModel, command);

            command.UserName = User.Identity.Name;

            try
            {
                await CommandSender.Send(command);
            }
            catch (EntityNotFoundException<DataAccess.Shows.Show>)
            {
                throw new HttpErrorException(NotFound());
            }
        }

        /// <summary>
        /// Configures the mapper for entities owned by this controller.
        /// </summary>
        /// <param name="cfg">The mapper configuration builder.</param>
        [MapperStartup]
        public static void ConfigureMapper(IMapperConfigurationExpression cfg)
        {
            cfg.CreateMap<AddShowModel, AddShowCommand>();
            cfg.CreateMap<UpdateShowModel, UpdateShowCommand>();
            cfg.CreateMap<DeleteShowModel, DeleteShowCommand>();
        }
    }
}
