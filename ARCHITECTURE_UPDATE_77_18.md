# Update 77.18 — submission collection scoped to form definition

Changed collection route from GET /digital-processes/SubmissionResponse/data to
GET /digital-processes/FormResponse/data/{id:guid}, where id is FormDefinition.id.
Returns JSON array of reconstructed submission data across form versions,
filtering FormDefinition, FormVersion and Submission by SecurityOperations.Read.
The single-submission JSON endpoint remains unchanged. README updated.
