﻿using System.Threading.Tasks;
using AutoMapper;
using Common.Cqrs;
using Common.Startup;
using Logic.Services;
using Logic.Shows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Website.Infrastructure;
using Website.Infrastructure.Helpers;

namespace Website.Api.Shows
{
    /// <summary>
    /// API controller for managing shows.
    /// </summary>
    /// <seealso cref="Controller" />
    [Route("api/shows/commands")]
    [Produces(ContentTypes.ApplicationJson)]
    public class ShowsCommandController : Controller
    {
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
        [Route("add")]
        [Authorize]
        public async Task<ShowAddedModel> Add([FromBody]AddShowModel commandModel)
        {
            ModelState.ThrowIfNotValid();

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
        [Route("update")]
        [Authorize]
        public async Task Update([FromBody]UpdateShowModel commandModel)
        {
            ModelState.ThrowIfNotValid();

            var command = new UpdateShowCommand();
            Mapper.Map(commandModel, command);
            command.UserName = User.Identity.Name;

            await CommandSender.Send(command);
        }

        /// <summary>
        /// Marks an existing show as deleted.
        /// </summary>
        /// <param name="commandModel">Contains information about the show to delete.</param>
        /// <response code="200">Show has been deleted.</response>
        /// <response code="400">Request is invalid or doesn't pass validation.</response>
        /// <response code="404">Show of provided ID doesn't exist.</response>
        [HttpPost]
        [Route("delete")]
        [Authorize]
        public async Task Delete([FromBody]DeleteShowModel commandModel)
        {
            ModelState.ThrowIfNotValid();

            var command = new DeleteShowCommand();
            Mapper.Map(commandModel, command);

            command.UserName = User.Identity.Name;

            await CommandSender.Send(command);
        }

        /// <summary>
        /// Adds a new picture to the show.
        /// </summary>
        /// <param name="request">Contains information about the picture to add.</param>
        /// <response code="200">Picture was added.</response>
        /// <response code="400">Request is malformed or invalid.</response>
        /// <response code="404">Show of provided ID doesn't exist.</response>
        [HttpPost]
        [Route("add-picture")]
        [Authorize]
        [Consumes(ContentTypes.MultipartFormData, ContentTypes.ApplicationXWwwFormUrlEncoded)]
        public async Task<ShowPictureAddedModel> AddPicture([FromForm] AddShowPictureModel request)
        {
            ModelState.ThrowIfNotValid();

            var command = new AddShowPictureCommand();
            Mapper.Map(request, command);

            command.FileName = request.Picture.FileName;
            command.ShowPictureId = await IdService.GenerateId();
            command.UserName = User.Identity.Name;

            using (var stream = request.Picture.OpenReadStream())
            {
                command.FileStream = stream;
                await CommandSender.Send(command);
            }

            var result = new ShowPictureAddedModel {ShowPictureId = command.ShowPictureId};
            return result;
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
            cfg.CreateMap<AddShowPictureModel, AddShowPictureCommand>();
        }
    }
}
