import {
    beforeEach,
    describe,
    expect,
    it,
    vi,
    type MockedObject,
} from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ErrorService } from './error.service';
import { ToastrService } from 'ngx-toastr';

describe('ErrorService', () => {
    let service: ErrorService;
    let toastrService: MockedObject<ToastrService>;

    beforeEach(() => {
        toastrService = {
            error: vi.fn().mockName('ToastrService.error'),
        } as MockedObject<ToastrService>;

        TestBed.configureTestingModule({
            providers: [
                ErrorService,
                { provide: ToastrService, useValue: toastrService },
            ],
        });

        service = TestBed.inject(ErrorService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should call toastr.error with comma-separated error messages', () => {
        // Arrange
        const errorResponse = {
            response: JSON.stringify({
                errors: {
                    field1: ['Error1', 'Error2'],
                    field2: ['Error3'],
                },
            }),
        };

        const defaultMessage = 'Default error message';

        // Act
        service.handleError(errorResponse, defaultMessage);

        // Assert
        const expectedMessage = 'Error1, Error2, Error3';
        expect(toastrService.error).toHaveBeenCalledTimes(1);
        expect(toastrService.error).toHaveBeenCalledWith(
            expectedMessage,
            'Error',
        );
    });

    it('should call toastr.error with default message if no errors present', () => {
        // Arrange
        const errorResponse = {
            response: JSON.stringify({}),
        };

        const defaultMessage = 'Default error message';

        // Act
        service.handleError(errorResponse, defaultMessage);

        // Assert
        expect(toastrService.error).toHaveBeenCalledTimes(1);

        // Assert
        expect(toastrService.error).toHaveBeenCalledWith(
            defaultMessage,
            'Error',
        );
    });
});
