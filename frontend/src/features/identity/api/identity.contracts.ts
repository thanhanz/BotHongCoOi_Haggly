export interface RegisterBuyerRequest {
  email: string;
  phoneNumber: string;
  password: string;
  fullName: string;
}

export interface Registration {
  userId: string;
  email: string;
  status: string;
  role: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface Login {
  userId: string;
  email: string;
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  roles: string[];
}
