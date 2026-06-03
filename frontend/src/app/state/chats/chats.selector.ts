import { createFeatureSelector } from '@ngrx/store';
import { IChatDto } from '../../api-client/api-client';

export const selectChats = createFeatureSelector<readonly IChatDto[]>('chats');
