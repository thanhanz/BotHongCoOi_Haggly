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
