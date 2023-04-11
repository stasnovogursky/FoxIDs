﻿using FoxIDs.SeedTool.Logic;
using FoxIDs.SeedTool.Model;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UrlCombineLib;

namespace FoxIDs.SeedTool.SeedLogic
{
    public class RiskPasswordSeedLogic
    {
        private const int riskPasswordMoreThenBreachesCount = 100;
        private const int uploadRiskPasswordBlockSize = 1000;
        private readonly SeedSettings settings;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly AccessLogic accessLogic;

        public RiskPasswordSeedLogic(SeedSettings settings, IHttpClientFactory httpClientFactory, AccessLogic accessLogic)
        {
            this.settings = settings;
            this.httpClientFactory = httpClientFactory;
            this.accessLogic = accessLogic;
        }

        public string PasswordRiskListApiEndpoint => UrlCombine.Combine(settings.FoxIDsMasterControlApiEndpoint, "!riskpassword");
        public string PasswordRiskFirstListApiEndpoint => UrlCombine.Combine(settings.FoxIDsMasterControlApiEndpoint, "!riskpasswordfirst");

        public async Task SeedAsync()
        {
            Console.WriteLine("Uploading risk passwords");
            var totalCount = 0;
            var riskPasswords = new List<RiskPasswordApiModel>();            
            using (var streamReader = File.OpenText(settings.PwnedPasswordsPath))
            {
                var i = 0;
                while (streamReader.Peek() >= 0)
                {
                    i++;
                    var split = streamReader.ReadLine().Split(':');
                    var breachesCount = Convert.ToInt32(split[1]);
                    if (breachesCount >= riskPasswordMoreThenBreachesCount)
                    {
                        riskPasswords.Add(new RiskPasswordApiModel { PasswordSha1Hash = split[0], Count = breachesCount });
                        if (riskPasswords.Count >= uploadRiskPasswordBlockSize)
                        {
                            totalCount += riskPasswords.Count;
                            await SavePasswordsRiskListAsync(await accessLogic.GetAccessTokenAsync(), riskPasswords);
                            Console.WriteLine($"Risk passwords uploaded '{totalCount}'");
                            riskPasswords = new List<RiskPasswordApiModel>();
                        }
                    }
                }
            }

            if (riskPasswords.Count > 0)
            {
                Console.WriteLine("Uploading the last risk passwords");
                await SavePasswordsRiskListAsync(await accessLogic.GetAccessTokenAsync(), riskPasswords);
            }

            Console.WriteLine($"All '{totalCount}' risk passwords uploaded");
        }

        public async Task DeleteAllAsync()
        {
            Console.WriteLine("Delete all risk passwords");
            var totalCount = 0;
            while (true)
            {
                var riskPasswords = await GetPasswordsRiskFirstListAsync(await accessLogic.GetAccessTokenAsync());
                if(riskPasswords?.Count > 0)
                {
                    totalCount = totalCount + riskPasswords.Count();
                    await DeletePasswordsRiskListAsync(await accessLogic.GetAccessTokenAsync(), riskPasswords.Select(r => r.PasswordSha1Hash).ToList());
                    Console.WriteLine($"Risk passwords deleted '{totalCount}'");
                }
                else
                {
                    break;
                }
            }

            Console.WriteLine($"All '{totalCount}' risk passwords deleted");
        }

        private async Task SavePasswordsRiskListAsync(string accessToken, List<RiskPasswordApiModel> riskPasswords)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);
            using var response = await client.UpdateJsonAsync(PasswordRiskListApiEndpoint, new RiskPasswordRequestApiModel { RiskPasswords = riskPasswords });
            await response.ValidateResponseAsync();
        }

        private async Task<List<RiskPasswordApiModel>> GetPasswordsRiskFirstListAsync(string accessToken)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);
            using var response = await client.GetAsync(PasswordRiskFirstListApiEndpoint);
            await response.ValidateResponseAsync();
            var result = await response.Content.ReadAsStringAsync();
            return result.ToObject<List<RiskPasswordApiModel>>();   
        }

        private async Task DeletePasswordsRiskListAsync(string accessToken, List<string> passwordSha1Hashs)
        {
            var body = new RiskPasswordDeleteApiModel { PasswordSha1Hashs = passwordSha1Hashs };

            var request = new HttpRequestMessage();
            request.Headers.Authorization = new AuthenticationHeaderValue(IdentityConstants.TokenTypes.Bearer, accessToken);
            var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(body));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            request.Content = content;
            request.Method = new HttpMethod("DELETE");
            request.RequestUri = new Uri(PasswordRiskListApiEndpoint);
            var client = httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            await response.ValidateResponseAsync();
        }
    }
}
