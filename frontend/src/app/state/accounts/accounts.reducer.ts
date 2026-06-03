import { createReducer, on } from '@ngrx/store';
import { IAccountDto } from '../../api-client/api-client';
import { accountsActions } from './accounts.actions';

export const initialState: readonly IAccountDto[] = null;

export const accountsReducer = createReducer(
    initialState,
    on(accountsActions.init, (_state, { accounts }) => accounts),
);
