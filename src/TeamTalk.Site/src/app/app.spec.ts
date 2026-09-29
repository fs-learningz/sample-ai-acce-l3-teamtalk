import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('renders a router outlet as the application shell', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const outlet = (fixture.nativeElement as HTMLElement).querySelector('router-outlet');
    expect(outlet).not.toBeNull();
  });

  it('has the site title', () => {
    const fixture = TestBed.createComponent(App);

    expect((fixture.componentInstance as unknown as { title: () => string }).title()).toBe('TeamTalk.Site');
  });
});
