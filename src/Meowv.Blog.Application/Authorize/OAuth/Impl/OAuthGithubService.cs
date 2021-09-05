﻿using Meowv.Blog.Domain.Users;
using Meowv.Blog.Dto.Authorize;
using Meowv.Blog.Extensions;
using Meowv.Blog.Options.Authorize;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace Meowv.Blog.Authorize.OAuth.Impl
{
    public class OAuthGithubService : OAuthServiceBase<GithubOptions, AccessTokenBase, UserInfoBase>
    {
        public override async Task<string> GetAuthorizeUrl(string state)
        {
            var param = BuildAuthorizeUrlParams(state);
            var url = $"{Options.Value.AuthorizeUrl}?{param.ToQueryString()}";

            return await Task.FromResult(url);
        }

        public override async Task<User> GetUserByOAuthAsync(string type, string code, string state)
        {
            var accessToken = await GetAccessTokenAsync(code, state);
            var userInfo = await GetUserInfoAsync(accessToken);

            return await UserService.CreateUserAsync(userInfo.Login, type, userInfo.Id, userInfo.Name, userInfo.Avatar, userInfo.Email);
        }

        public override async Task<AccessTokenBase> GetAccessTokenAsync(string code, string state)
        {
            var param = BuildAccessTokenParams(code, state);

            var content = new StringContent(param.ToQueryString());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            using var client = HttpClient.CreateClient();
            var httpResponse = await client.PostAsync(Options.Value.AccessTokenUrl, content);

            var response = await httpResponse.Content.ReadAsStringAsync();

            var qscoll = HttpUtility.ParseQueryString(response);

            return new AccessTokenBase
            {
                AccessToken = qscoll["access_token"],
                Scope = qscoll["scope"],
                TokenType = qscoll["token_type"]
            };
        }

        public override async Task<UserInfoBase> GetUserInfoAsync(AccessTokenBase accessToken)
        {
            using var client = HttpClient.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"token {accessToken.AccessToken}");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36 Edg/87.0.664.66");

            var response = await client.GetStringAsync(Options.Value.UserInfoUrl);

            var userInfo = response.DeserializeToObject<UserInfoBase>();
            return userInfo;
        }

        protected Dictionary<string, string> BuildAuthorizeUrlParams(string state)
        {
            return new Dictionary<string, string>
            {
                ["client_id"] = Options.Value.ClientId,
                ["redirect_uri"] = Options.Value.RedirectUrl,
                ["scope"] = Options.Value.Scope,
                ["state"] = state
            };
        }

        protected Dictionary<string, string> BuildAccessTokenParams(string code, string state)
        {
            return new Dictionary<string, string>()
            {
                ["client_id"] = Options.Value.ClientId,
                ["client_secret"] = Options.Value.ClientSecret,
                ["redirect_uri"] = Options.Value.RedirectUrl,
                ["code"] = code,
                ["state"] = state
            };
        }
    }
}