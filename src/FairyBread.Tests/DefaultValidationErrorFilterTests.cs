using System;
using System.Collections.Generic;
using FluentValidation;
using HotChocolate;
using Xunit;

namespace FairyBread.Tests
{
    public class DefaultValidationErrorFilterTests
    {
        [Fact]
        public void Should_Handle_ValidationException()
        {
            // Arrange
            var error = new AnError().WithException(new ValidationException("some msg"));

            var defaultValidationErrorFilter = new ValidationErrorFilter();

            // Act / Assert
            Assert.Throws<NotImplementedException>(() => defaultValidationErrorFilter.OnError(error));
        }

        [Fact]
        public void Should_Ignore_OtherException()
        {
            // Arrange
            var error = new AnError().WithException(new Exception("some msg"));

            var defaultValidationErrorFilter = new ValidationErrorFilter();

            // Act / Assert
            defaultValidationErrorFilter.OnError(error);
        }

        [Fact]
        public void Should_Ignore_CertainValidationException()
        {
            // Arrange
            var error = new AnError().WithException(new Exception("some msg"));

            var defaultValidationErrorFilter = new ValidationErrorFilter(ex => false);

            // Act / Assert
            defaultValidationErrorFilter.OnError(error);
        }

        class AnError : IError
        {
            public string Message => throw new NotImplementedException();

            public string? Code => throw new NotImplementedException();

            public Path? Path => throw new NotImplementedException();

            public IReadOnlyList<Location>? Locations => throw new NotImplementedException();

            public IReadOnlyDictionary<string, object?>? Extensions => throw new NotImplementedException();

            public Exception? Exception { get; private set; }
            public IError WithException(Exception? exception)
            {
                Exception = exception;
                return this;
            }

            public IError RemoveCode() => throw new NotImplementedException();
            public IError RemoveException() => throw new NotImplementedException();
            public IError RemoveExtension(string key) => throw new NotImplementedException();
            public IError RemoveExtensions() => throw new NotImplementedException();
            public IError RemoveLocations() => throw new NotImplementedException();
            public IError RemovePath() => throw new NotImplementedException();
            public IError SetExtension(string key, object? value) => throw new NotImplementedException();
            public IError WithCode(string? code) => throw new NotImplementedException();
            public IError WithExtensions(IReadOnlyDictionary<string, object?> extensions) => throw new NotImplementedException();
            public IError WithLocations(IReadOnlyList<Location>? locations) => throw new NotImplementedException();
            public IError WithMessage(string message) => throw new NotImplementedException();
            public IError WithPath(Path? path) => throw new NotImplementedException();
            public IError WithPath(IReadOnlyList<object>? path) => throw new NotImplementedException();
        }
    }
}
