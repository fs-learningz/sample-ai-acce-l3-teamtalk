export interface AuthResponse {
  userId: string;
  email: string;
  displayName: string;
  token: string;
}

export interface RegisterRequest {
  email: string;
  displayName: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}
