#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPORT="${1:-$ROOT/artifacts/nuget-audit.json}"
if [[ "$REPORT" != /* ]]; then
    REPORT="$ROOT/$REPORT"
fi
mkdir -p "$(dirname "$REPORT")"

printf '%s\n' 'The attribute-based architecture auditor has been removed. Running build, tests and NuGet audit.'
status=0
if dotnet build "$ROOT/SignFakeArchitecture.sln" --configuration Release; then
    # Relational suites share one test server; avoid concurrent project-level
    # database creation/cleanup competing with the authorization regressions.
    if ! dotnet test "$ROOT/SignFakeArchitecture.sln" --configuration Release --no-build --no-restore --maxcpucount:1 \
        --logger "trx;LogFilePrefix=validation" --results-directory "$ROOT/artifacts/test-results"; then
        status=1
    fi
else
    printf '%s\n' 'Build failed; tests were not run.' >&2
    status=1
fi

if dotnet list "$ROOT/SignFakeArchitecture.sln" package --vulnerable --include-transitive --format json > "$REPORT"; then
    cat "$REPORT"
else
    printf '%s\n' 'NuGet vulnerability audit failed.' >&2
    status=1
fi
printf 'NuGet vulnerability report (not an architecture audit): %s\n' "$REPORT"
exit "$status"
