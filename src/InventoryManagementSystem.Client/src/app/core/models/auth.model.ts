export interface LoginModel {
  usernameOrEmail: string;
  password: string;
  rememberMe?: boolean;
}

export interface AuthResponseModel {
  userId: string;
  userName: string;
  email: string;
  roles: string[];
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface RefreshTokenModel {
  accessToken: string;
  refreshToken: string;
}
