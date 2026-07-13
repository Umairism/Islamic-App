# Islamic Research Platform API Specification (v1)

This document specifies the read-only REST endpoints for retrieving Qur'an data. All successful responses wrap the payload in a standard envelope.

---

## Standard Response Envelope

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

For list responses, pagination metadata is supplied if paginated:

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

## Standard Error Response Format (RFC 7807)

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
      "message": "Surah number must be a valid integer between 1 and 114."
    }
  ]
}
```

---

## Endpoints

### 1. Health Status
Check backend service health, database connectivity, dataset status, system uptime, and correlation context.

* **URL**: `/health`
* **Method**: `GET`
* **Auth Required**: No

#### Response: `200 OK`
```json
{
  "status": "Healthy",
  "database": "Healthy",
  "dataset": "Healthy",
  "latestImport": "2026-07-12T21:59:46.000Z",
  "uptime": "01:23:45",
  "version": "0.2.0",
  "correlationId": "c85d7b5f-5d46-4e56-b072-bc32fa5a7e93"
}
```

#### Response: `503 Service Unavailable`
```json
{
  "status": "Unhealthy",
  "database": "Unhealthy",
  "dataset": "Error",
  "error": "Failed to connect to database.",
  "correlationId": "c85d7b5f-5d46-4e56-b072-bc32fa5a7e93",
  "timestamp": "2026-07-13T05:00:00.000Z"
}
```

---

### 2. API Version Details
Expose active version metadata for deployments.

* **URL**: `/api/version`
* **Method**: `GET`
* **Auth Required**: No

#### Response: `200 OK`
```json
{
  "api": "1.0",
  "dataset": "Quran 3.1.2",
  "build": "0.2.0"
}
```

---

### 3. List All Surahs
Retrieve list of all 114 Surahs. Supports pagination.

* **URL**: `/api/v1/quran/surahs`
* **Method**: `GET`
* **Auth Required**: No
* **Query Parameters**:
  * `page` (integer, optional, default: 1): Page number.
  * `pageSize` (integer, optional, default: 20): Number of records per page.

#### Response: `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "number": 1,
      "arabicName": "الفاتحة",
      "transliteration": "Al-Fatihah",
      "englishName": "The Opening",
      "revelationType": "meccan",
      "totalVerses": 7
    },
    {
      "number": 2,
      "arabicName": "البقرة",
      "transliteration": "Al-Baqarah",
      "englishName": "The Cow",
      "revelationType": "medinan",
      "totalVerses": 286
    }
  ],
  "meta": {
    "version": "1.0",
    "pagination": {
      "page": 1,
      "pageSize": 2,
      "total": 114
    }
  }
}
```

---

### 4. Get Surah Metadata
Retrieve metadata for a specific Surah by its number (1 to 114).

* **URL**: `/api/v1/quran/surahs/{number}`
* **Method**: `GET`
* **Auth Required**: No
* **Path Parameters**:
  * `number` (integer, required): The Surah number.

#### Response: `200 OK`
```json
{
  "success": true,
  "data": {
    "number": 1,
    "arabicName": "الفاتحة",
    "transliteration": "Al-Fatihah",
    "englishName": "The Opening",
    "revelationType": "meccan",
    "totalVerses": 7
  },
  "meta": {
    "version": "1.0",
    "pagination": null
  }
}
```

---

### 5. Get Verse
Retrieve a specific verse (ayah) by Surah number and Ayah number, including base Arabic text, clean Arabic, English transliteration, canonical reference, and optional translations.

* **URL**: `/api/v1/quran/verses/{surah}/{ayah}`
* **Method**: `GET`
* **Auth Required**: No
* **Path Parameters**:
  * `surah` (integer, required): Surah number (1 to 114).
  * `ayah` (integer, required): Ayah number (1-indexed inside the Surah).
* **Query Parameters**:
  * `translations` (string, optional): Comma-separated list of language codes to include (e.g. `en,ur,es`). If omitted, all available translations are returned.

#### Response: `200 OK`
```json
{
  "success": true,
  "data": {
    "reference": {
      "type": "Quran",
      "reference": "2:255",
      "globalIndex": 262,
      "language": "ar"
    },
    "surahNumber": 2,
    "ayahNumber": 255,
    "arabicText": "ٱللَّهُ لَآ إِلَٰهَ إِلَّا هُوَ ٱلۡحَيُّ ٱلۡقَيُّومُۚ ...",
    "arabicCleaned": "الله لا إله إلا هو الحي القيوم ...",
    "transliteration": "Allahu la ilaha illa huwa alhayyu alqayyoomu ...",
    "translations": [
      {
        "language": "en",
        "translator": "Sahih International",
        "text": "Allah - there is no deity except Him, the Ever-Living, the Sustainer of [all] existence..."
      },
      {
        "language": "ur",
        "translator": "Abul A'la Maududi",
        "text": "اللہ، وہ زندہ جاوید ہستی، جو تمام کائنات کو سنبھالے ہوئے ہے..."
      }
    ]
  },
  "meta": {
    "version": "1.0",
    "pagination": null
  }
}
```

---

### 6. List Translations Info
Retrieve a list of all supported translation languages and their respective translators. Supports pagination.

* **URL**: `/api/v1/quran/translations`
* **Method**: `GET`
* **Auth Required**: No
* **Query Parameters**:
  * `page` (integer, optional, default: 1): Page number.
  * `pageSize` (integer, optional, default: 20): Page size.

#### Response: `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "language": "bn",
      "translator": "Bengali Translator"
    },
    {
      "language": "en",
      "translator": "Sahih International"
    }
  ],
  "meta": {
    "version": "1.0",
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "total": 10
    }
  }
}
```

---

### 7. List Datasets Info (System Resource)
Retrieve a list of all registered dataset versions imported in the database. Supports pagination.

* **URL**: `/api/v1/system/datasets`
* **Method**: `GET`
* **Auth Required**: No

#### Response: `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "id": "quran-3.1.2",
      "name": "Quran-JSON",
      "edition": "Complete Arabic text with translation & transliteration",
      "version": "3.1.2",
      "source": "https://github.com/risan/quran-json",
      "license": "CC-BY-4.0",
      "checksum": "d8a8adff387f60ce3ff7dbe3238dd9b27120bfe29d8fcb07ad2e89cad37cefd4",
      "importedAt": "2026-07-13T05:00:00.000Z"
    }
  ],
  "meta": {
    "version": "1.0",
    "pagination": null
  }
}
```

---

### 8. List Import Sessions (System Resource)
Retrieve audit logs detailing past ETL pipeline executions. Supports pagination.

* **URL**: `/api/v1/system/imports`
* **Method**: `GET`
* **Auth Required**: No
* **Query Parameters**:
  * `page` (integer, optional, default: 1): Page number.
  * `pageSize` (integer, optional, default: 20): Page size.

#### Response: `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "id": "session-1783919149983-uf0eg7",
      "datasetId": "quran-3.1.2",
      "startedAt": "2026-07-13T05:05:49.000Z",
      "completedAt": "2026-07-13T05:05:56.000Z",
      "status": "SUCCESS",
      "durationMs": 6915,
      "warnings": [],
      "errors": [],
      "memoryUsageMb": 234.93
    }
  ],
  "meta": {
    "version": "1.0",
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "total": 1
    }
  }
}
```
