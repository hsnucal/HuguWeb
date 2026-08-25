# Document / object storage

> **Status:** Accepted — ARCH-01 (2026-08-25).

## Split

| Owner | Concern |
|-------|---------|
| Domain (HR) | `DocumentType`, `EmployeeId`, metadata, sensitivity |
| Storage | Object key, stream, content type, length |

Do not put binary/base64 on domain entities. Do not create a generic “Document” aggregate for every module.

## Current

`IEmployeePhotoStorage` + `FileSystemEmployeePhotoStorage` (Development). `EmployeePhoto` stores `StorageKey`, content type, size — not bytes.

## Later

S3 / Azure Blob / compatible object storage behind the same interface. Do not integrate cloud now. HR domain must not change when the provider changes.
