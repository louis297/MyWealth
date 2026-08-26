export type Adviser = {
  id: number
  name: string
  email: string
  isEnabled: boolean
}

export type CreateAdviserRequest = {
  name: string
  email: string
  password: string
}

export type UpdateAdviserRequest = {
  id: number
  name?: string
  isEnabled?: boolean
}

export type CreatedId = {
  id: number
}

export type GetAdvisersArgs = {
  page?: number
  pageSize?: number
  isEnabled?: boolean
  search?: string
}
