﻿using System;
using System.Collections.Generic;
using System.Text;

#nullable disable
namespace domain_extensions.Core.Result
{
  public class Result<T>
  {

    public readonly T Value;
    public readonly string Error;

    Result(T value, string err)
    {
      Value = value;
      Error = err;
    }

    public bool IsSuccess => string.IsNullOrEmpty(Error);

    // TODO rsx default error
    public T Unwrap() => IsSuccess 
      ? Value : throw new OperationException(new Error(Error ?? string.Empty));


    // --- Builders ------------------------------------------------------------
    public static Result<T> OK(T value) => 
      new Result<T>(value, string.Empty);

    public static Result<T> FAIL(string error) =>
      new Result<T>(default, error);

  }
}
