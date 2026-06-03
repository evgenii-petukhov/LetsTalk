import { createFeatureSelector } from '@ngrx/store';
import { Message } from '../../models/message';

export const selectMessages =
    createFeatureSelector<readonly Message[]>('messages');
