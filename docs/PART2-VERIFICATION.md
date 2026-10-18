# Part 2 Behaviour Verification

Verified on 18 October 2026 against an isolated SQLite database.

## Automated checks

- Priority processing: critical readings precede warnings and normal readings; equal priorities preserve arrival order.
- Commands: start, stop and toggle update the simulated device control state.
- Undo: restores the previous control state, proceeds backwards through commands, and rejects requests when nothing remains to undo.
- Dashboard: category and severity searches find matching devices; unknown searches return no matches.
- Errors: repeated readings for the same device metric produce one error label; a normal reading clears that metric's active error.
- Recommendations: abnormal readings produce relevant actions; repeating a command produces a frequency-based suggestion.
- Telemetry API: 50 simultaneous submissions return distinct saved reading IDs.
- Connection simulation: false/true connected readings change the device to Disconnected/Online.
- Browser: sending a command, Undo, search and clear, three-second refresh, and draft retention across navigation all pass without page errors.

## Run the regression tests

    dotnet build PROG7312POE.sln
    dotnet test PROG7312POE.Tests --no-restore

Tests use SQLite in memory and do not modify the application database.

## Demo checklist

1. Register a device and open Commands.
2. Submit a moisture reading of 10 from Telemetry; wait for Commands to refresh.
3. Search for Critical, then clear the search.
4. Select a suggested action and submit it. The simulated control state should change to On.
5. Submit Stop pump. The state should change to Off.
6. Undo twice: the state returns to On and then its original value.
7. Submit a moisture reading of 50 and confirm that the active moisture issue clears.
8. Enter a draft command, visit Telemetry, and return to Commands. The draft should remain.

## Simulation boundaries

Commands update a persisted simulated control state; no physical device is contacted.
Timed commands do not implement a hardware timer. Pending telemetry queues are held
in memory and are not durable across a process crash. Dashboard telemetry lists show
recent processed history, while ingestion uses a separate shared priority/FIFO work queue.
The dashboard refreshes every three seconds rather than using a push connection.
