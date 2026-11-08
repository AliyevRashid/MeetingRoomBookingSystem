using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace common_library;

public  class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public int ErrorCode { get; }

    protected Result(bool isSuccess, int errorCode)
    {
        if (isSuccess && errorCode != 0 ||
            !isSuccess && errorCode == 0)
        {
            throw new InvalidOperationException("Invalid error state for Result.");
        }

        IsSuccess = isSuccess;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, 0);
    public static Result Failure(int errorCode) => new(false, errorCode);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue? value, bool isSuccess, int errorCode) : base(isSuccess, errorCode)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result cannot be accessed.");

    public static Result<TValue> Success(TValue value) => new(value, true, 0);
    public static new Result<TValue> Failure(int errorCode) => new(default, false, errorCode);
}
