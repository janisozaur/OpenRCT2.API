﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using OpenRCT2.Api.Client;
using OpenRCT2.Api.Client.Models;
using OpenRCT2.Content.Models;
using OpenRCT2.Content.Services;

namespace OpenRCT2.Content.Pages
{
    public partial class Upload
    {
        [Inject]
        private AuthorisationService Auth { get; set; }

        [Inject]
        private OpenRCT2ApiService Api { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }

        private readonly ContentEditFormModel contentEditForm = new ContentEditFormModel();

        protected override void OnInitialized()
        {
            if (!Auth.IsPower)
            {
                Navigation.NavigateTo("/");
            }

            contentEditForm.AvailableOwners = new[] { Auth.Name };
            contentEditForm.Owner = Auth.Name;
            contentEditForm.Visibility = ContentVisibility.Public;
            contentEditForm.SubmitButtonText = "Upload";
        }

        private async Task OnValidate()
        {
            var response = await Api.Client.Content.VerifyName(contentEditForm.Owner, contentEditForm.Name);
            if (response.Valid)
            {
                contentEditForm.NameIsValid = true;
                contentEditForm.NameValidationMessage = null;
            }
            else
            {
                contentEditForm.NameIsValid = false;
                contentEditForm.NameValidationMessage = response.Message;
            }
            StateHasChanged();
        }

        private async Task OnSubmit()
        {
            if (contentEditForm.File == null || contentEditForm.Image == null)
            {
                contentEditForm.ValidationMessage = "You must upload a file and image.";
                StateHasChanged();
            }
            else
            {
                var maxAllowedSize = 8 * 1024 * 1024; // 8 MiB
                var maxAllowedImageSize = 4 * 1024 * 1024; // 4 MiB

                var request = new UploadContentRequest
                {
                    Owner = contentEditForm.Owner,
                    Name = contentEditForm.Name,
                    Description = contentEditForm.Description,
                    Visibility = contentEditForm.Visibility,
                    File = contentEditForm.File.OpenReadStream(maxAllowedSize),
                    FileName = contentEditForm.File.Name,
                    Image = contentEditForm.Image.OpenReadStream(maxAllowedImageSize),
                    ImageFileName = contentEditForm.Image.Name
                };
                try
                {
                    var response = await Api.Client.Content.Upload(request);
                    Navigation.NavigateTo($"/{response.Owner}/{response.Name}");
                }
                catch (OpenRCT2ApiClientStatusCodeException<UploadContentResponse> ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        contentEditForm.ValidationMessage = "You must be logged in to upload content.";
                    }
                    else
                    {
                        contentEditForm.ValidationMessage = ex.Content.Message;
                    }
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    contentEditForm.ValidationMessage = ex.Message;
                    StateHasChanged();
                }
            }
        }
    }
}
