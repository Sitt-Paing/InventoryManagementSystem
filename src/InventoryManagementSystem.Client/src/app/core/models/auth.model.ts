export interface LoginModel {
  usernameOrEmail: string;
  password: string;
  rememberMe?: boolean;
  theme?: string;
  language?: string;
}

export interface AuthResponseModel {
  userId?: string;
  userName?: string;
  email?: string;
  roles?: string[];
  theme?: string;
  language?: string;
  rememberMe?: boolean;
  accessToken?: string;
  refreshToken?: string;
  expiresAt?: string;
}

export interface RefreshTokenModel {
  accessToken?: string;
  refreshToken?: string;
}

