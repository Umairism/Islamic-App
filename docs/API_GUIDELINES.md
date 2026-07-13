# API Design and Implementation Guidelines

This document outlines the strict architectural standards and rules for the Islamic Research Platform REST API.

---

## 1. Architectural Rules

* **Controllers are Shells**: Controllers must contain **zero** business or query logic. They only map HTTP requests to application services and return standardized responses.
* **Async Only**: Every controller action, service method, and repository query must be asynchronous (`async` / `await`). There must be no synchronous database calls.
* **Cancellation Support**: Every endpoint, application service, and repository method must accept a `CancellationToken` and propagate it to all downstream database calls and HTTP clients.
* **Response Envelope**: Every successful API response must be wrapped in a consistent response envelope. Direct array or entity returns are forbidden.
* **No Direct Model Exposure**: Database entities (Persistence Models) must never be returned from controllers. DTOs must be utilized for all response objects.
* **API Versioning**: Every API controller must be decorated with explicit `[ApiVersion("1.0")]` (or other appropriate version) attributes. Route mapping must use version variables: `[Route("api/v{version:apiVersion}/quran/[controller]")]`.
* **Correlation ID**: Every request must pass through a Correlation ID middleware. The middleware must look for an `X-Correlation-ID` header, generate one if missing, attach it to the logger context (Serilog log context), and return it in the response headers.

---

## 2. Standard Response Envelope

All successful responses must follow this layout:

```json
{
  "success": true,
  "data": {},
  "meta": {
    "version": "1.0",
    "pagination": null
  }
}
```

For paginated lists, the `pagination` metadata must be populated:

```json
{
  "success": true,
  "data": [],
  "meta": {
    "version": "1.0",
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "total": 114
    }
  }
}
```

---

## 3. Reference DTO Structure

To ensure canonical reference formatting across different types of evidence (Qur'an, Hadith, Tafsir, etc.), reference payloads must return a consistent layout:

```json
{
  "type": "Quran",
  "reference": "2:255",
  "globalIndex": 262,
  "language": "ar"
}
```

Or for Hadith:

```json
{
  "type": "Hadith",
  "reference": "Bukhari 54",
  "collection": "Bukhari",
  "book": 3,
  "hadithNumber": 54
}
```

---

## 4. Error Responses (RFC 7807)

Errors must be returned using the `RFC 7807` Problem Details standard. The JSON response must look like this:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "The requested resource was not found.",
  "instance": "/api/v1/quran/surahs/150",
  "timestamp": "2026-07-13T05:00:00.000Z",
  "errors": []
}
```

If validation fails, the `errors` array lists the fields and error messages:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/quran/verses/150/1",
  "timestamp": "2026-07-13T05:00:00.000Z",
  "errors": [
    {
      "field": "surah",
      "message": "Surah number must be between 1 and 114."
    }
  ]
}
```

---

## 5. Repository & Specification Pattern

To keep repositories focused and prevent "God Queries", repositories must accept generic `ISpecification<T>` interfaces to filter and shape data:

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>> OrderBy { get; }
    Expression<Func<T, object>> OrderByDescending { get; }
}
```

---

## 6. Swagger and XML Documentation

* **XML Comments**: Write XML doc comments on all public endpoints and request parameters. Swagger must be configured to read these XML comments.
* **Model Schemas**: Every response model must have descriptive schemas in Swagger UI.

---

## 7. Serialization and Naming Policy

* **JSON Naming**: Use the camelCase naming policy for all JSON serialization.
* **DateTimes**: DateTimes must always be formatted in ISO 8601 UTC format.
