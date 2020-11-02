﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace villa_configuration.Clients
{


  // --- Settings --------------------------------------------------------------
  public class TinkSettings
  { 
    public TinkPathSettings APIPath { get; set; }
  }

  public class TinkPathSettings
  { 
    public string SearchTransactions { get; set; }
    public string OAuthToken { get; set; }
  }

}
