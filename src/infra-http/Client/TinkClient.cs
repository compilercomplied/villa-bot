﻿using AutoMapper;
using domain_business.Core.Product.Providers;
using domain_business.Core.Category.Providers;
using domain_business.Core.Transaction;
using domain_business.Core.Transaction.Providers;
using domain_business.Usecases.ProviderSync;
using domain_extensions.Extensions;
using domain_extensions.Http.Result;
using domain_infra.Auth;
using domain_infra.FixedValues;
using infra_configuration.Clients;
using infra_http.Auth.OAuth.Contracts;
using infra_http.Client.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace infra_http.Client
{
  // --- Type Aliases ----------------------------------------------------------
  using TransactionResponse   = HttpResult<ProviderTransaction[], string>;
  using AccountResponse       = HttpResult<ProviderAccount[], string>;
  using CategoryListResponse  = HttpResult<ProviderCategory[], string>;
  using OAuthResponse         = HttpResult<OAuthCredentials, string>;

  // ---------------------------------------------------------------------------

  public class TinkClient : IAggregationProviderClient
  {

    #region services
    private readonly ILogger<TinkClient> _logger;
    private readonly TinkSettings _settings;
    private readonly HttpClient _client;
    private readonly IOAuthClient _oauth;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    #endregion services

    #region constructor
    public TinkClient(
      ILogger<TinkClient> logger,
      HttpClient client,
      IOptions<TinkSettings> settings,
      IOAuthClient oauth,
      IMapper mapper,
      IMemoryCache cache
    )
    {
      _oauth = oauth;

      _client = client;

      _logger = logger;
      _mapper = mapper;
      _cache = cache;

      _settings = settings.Value;

    }
    #endregion constructor

    public async Task<TransactionResponse> QueryTransactions(DataSyncRequest bodyArgs)
    {
      TransactionResponse result;


      var body = JsonConvert.SerializeObject(new
      {

        startDate = new DateTimeOffset(bodyArgs.From.ToUniversalTime())
          .ToUnixTimeMilliseconds(),

        endDate = new DateTimeOffset(DateTime.Today.AddDays(1).ToUniversalTime())
          .ToUnixTimeMilliseconds(),

          sort = "DATE",
          order = "ASC",

      });

      var path = _settings.APIPath.SearchTransactions;
      var request = new HttpRequestMessage(HttpMethod.Get, path)
      {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
      };


      var searchResponse = await _client.SendAsync(request);


      if (searchResponse.IsSuccessStatusCode)
      {

        var raw = await searchResponse.Content.ReadAsStringAsync();
        var payload = JsonConvert.DeserializeObject<TinkTransactionResponse>(raw);
        var transactionResponse = payload.Results.Select(r => r.Transaction);

        var transactions = _mapper.Map<ProviderTransaction[]>(transactionResponse);

        result = TransactionResponse.OK(transactions);

      }
      else
      {

        var errorMessage = await searchResponse.Content.ReadAsStringAsync();

        result = TransactionResponse.FAIL(
          HttpError<string>.FromRequest(errorMessage, searchResponse.StatusCode)
        );

      }

      return result;

    }

    public async Task<CategoryListResponse> ListCategories()
    {
      CategoryListResponse result;

      var path = _settings.APIPath.ListCategories;
      var request = new HttpRequestMessage(HttpMethod.Get, path);

      var response = await _client.SendAsync(request);


      if (response.IsSuccessStatusCode)
      {

        string raw = await response.Content.ReadAsStringAsync();
        var payload = JsonConvert.DeserializeObject<TinkCategory[]>(raw);

        var mappedCategories = _mapper.Map<ProviderCategory[]>(payload)
          // Remove expenses/income/transfer meta categories
          .Where(cat => !string.IsNullOrEmpty(cat.GroupID))
          .ToArray();

        result = CategoryListResponse.OK(mappedCategories);

      }
      else
      {

        var errorMessage = await response.Content.ReadAsStringAsync();

        result = CategoryListResponse.FAIL(
          HttpError<string>.FromRequest(errorMessage, response.StatusCode)
        );

      }

      return result;

    }

    public async Task<AccountResponse> ListAccounts()
    { 
      AccountResponse result;

      var path = _settings.APIPath.ListAccounts;
      var request = new HttpRequestMessage(HttpMethod.Get, path);

      var searchResponse = await _client.SendAsync(request);


      if (searchResponse.IsSuccessStatusCode)
      {

        var raw = await searchResponse.Content.ReadAsStringAsync();
        var payload = JsonConvert
          .DeserializeObject<TinkAccountResponse>(raw)?.Accounts 
          ?? Enumerable.Empty<TinkAccount>();

        var transactions = _mapper.Map<ProviderAccount[]>(payload);

        result = AccountResponse.OK(transactions);

      }
      else
      {

        var errorMessage = await searchResponse.Content.ReadAsStringAsync();

        result = AccountResponse.FAIL(
          HttpError<string>.FromRequest(errorMessage, searchResponse.StatusCode)
        );

      }

      return result;

    }

    // --- OAuth flow ----------------------------------------------------------
    public async Task<OAuthResponse> Authenticate(string code)
      => await _oauth.Authenticate(code);

    public async Task<OAuthResponse> RefreshAuth()
      => await _oauth.RefreshOAuth();

  }

}
