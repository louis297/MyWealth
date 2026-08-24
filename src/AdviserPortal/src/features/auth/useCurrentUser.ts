import { useAppSelector } from '../../app/hooks'
import { selectCurrentUser } from './authSlice'

export function useCurrentUser() {
  return useAppSelector(selectCurrentUser)
}
