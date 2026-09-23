import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WorkspaceIconComponent } from './workspace-icon.component';

describe('WorkspaceIconComponent', () => {
  let fixture: ComponentFixture<WorkspaceIconComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceIconComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkspaceIconComponent);
  });

  it('should render initials derived from the workspace name when there is no image', () => {
    fixture.componentRef.setInput('name', 'Product Engineering');
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent?.trim();
    expect(text).toBe('PE');
  });

  it('should render an image when an imageUrl is provided', () => {
    fixture.componentRef.setInput('name', 'Product Engineering');
    fixture.componentRef.setInput('imageUrl', 'blob:fake-url');
    fixture.detectChanges();

    const img = (fixture.nativeElement as HTMLElement).querySelector('img');
    expect(img?.getAttribute('src')).toBe('blob:fake-url');
  });
});
