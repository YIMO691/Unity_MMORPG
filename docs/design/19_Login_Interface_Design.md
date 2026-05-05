# Login Interface Design

This document defines the guest login interface for the MMORPG Demo Phase 1.

## Protocol Definition

The login protocol is defined in `proto/auth.proto`:

```protobuf
// Guest login request
message C2S_LoginReq {
  // Unique device identifier
  string device_id = 1;
  
  // Platform information (e.g., editor, windows, android)
  string platform = 2;
  
  // Application version
  string app_version = 3;
}

// Guest login response
message S2C_LoginRes {
  // Result code (0 for success)
  int32 code = 1;
  
  // Result message
  string message = 2;
  
  // Player unique identifier
  string player_id = 3;
  
  // Authentication token
  string token = 4;
  
  // Server timestamp in milliseconds
  int64 server_time = 5;
}
```

## HTTP Interface

### Endpoint

```
POST /api/auth/guest-login
```

### Request Headers

```
Content-Type: application/json
```

### Request Body Example

```json
{
  "deviceId": "dev-local-001",
  "platform": "editor",
  "appVersion": "0.1.0"
}
```

### Response Body Example (Success)

```json
{
  "code": 0,
  "message": "OK",
  "playerId": "player_demo_001",
  "token": "dev_token_demo_001",
  "serverTime": 1730000000000
}
```

### Response Body Example (Error)

```json
{
  "code": 1001,
  "message": "Invalid device ID format",
  "playerId": "",
  "token": "",
  "serverTime": 1730000000000
}
```

## Field Descriptions

### C2S_LoginReq Fields

- `device_id`: A unique identifier for the device making the request. For development, this could be a fixed string like "dev-local-001".
- `platform`: The platform where the game is running (e.g., "editor", "windows", "android", "ios").
- `app_version`: The version of the application making the request.

### S2C_LoginRes Fields

- `code`: An integer indicating the result of the operation. 0 means success.
- `message`: A human-readable description of the result.
- `player_id`: A unique identifier for the player assigned by the server.
- `token`: An authentication token that the client will use for subsequent requests.
- `server_time`: The current server time in milliseconds since Unix epoch.

## Implementation Notes

- This is a guest login system for Phase 1, no password or account verification is required.
- The server will assign a temporary player ID and token for the session.
- In future phases, this interface may be extended to support different authentication methods.
- The server maintains authority over player identity and session management.

## Security Considerations

- In Phase 1, tokens are for demonstration purposes only.
- In production, proper authentication and security measures will be implemented.
- Device IDs should be validated to prevent abuse.
