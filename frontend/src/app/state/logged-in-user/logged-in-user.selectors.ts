import { createFeatureSelector } from '@ngrx/store';
import { IProfileDto } from '../../api-client/api-client';

export const selectLoggedInUser =
    createFeatureSelector<IProfileDto>('loggedInUser');
