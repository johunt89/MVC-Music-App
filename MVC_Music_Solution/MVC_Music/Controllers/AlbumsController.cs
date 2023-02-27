using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVC_Music.Data;
using MVC_Music.Models;

namespace MVC_Music.Controllers
{
    [Authorize]
    public class AlbumsController : Controller
    {
        private readonly MusicContext _context;

        public AlbumsController(MusicContext context)
        {
            _context = context;
        }

        // GET: Albums
        [Authorize(Roles = "Staff, Supervisor, Admin, User")]
        public async Task<IActionResult> Index()
        {
            var musicContext = _context.Albums.Include(a => a.Genre);
            return View(await musicContext.ToListAsync());
        }

        // GET: Albums/Details/5
        [Authorize(Roles = "Staff, Supervisor, Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Albums == null)
            {
                return NotFound();
            }

            var album = await _context.Albums
                .Include(a => a.Genre)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (album == null)
            {
                return NotFound();
            }

            return View(album);
        }

        // GET: Albums/Create
        [Authorize(Roles = "Staff, Supervisor, Admin")]
        public IActionResult Create()
        {
            ViewData["GenreID"] = new SelectList(_context.Genres, "ID", "Name");
            return View();
        }

        // POST: Albums/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff, Supervisor, Admin")]
        public async Task<IActionResult> Create([Bind("ID,Name,YearProduced,Price,GenreID")] Album album)
        {
            if (ModelState.IsValid)
            {
                _context.Add(album);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GenreID"] = new SelectList(_context.Genres, "ID", "Name", album.GenreID);
            return View(album);
        }

        // GET: Albums/Edit/5
        [Authorize(Roles = "Staff, Supervisor, Admin")] // ** need to complete for staff
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Albums == null)
            {
                return NotFound();
            }

            var album = await _context.Albums.FindAsync(id);
            if (album == null)
            {
                return NotFound();
            }
            ViewData["GenreID"] = new SelectList(_context.Genres, "ID", "Name", album.GenreID);
            return View(album);
        }

        // POST: Albums/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff, Supervisor, Admin")] // need to complete for staff
        public async Task<IActionResult> Edit(int id, Byte[] RowVersion)
        {
            var albumToUpdate = await _context.Albums
                .Include(s => s.Songs)
                .Include(m => m.Genre)
                .FirstOrDefaultAsync(a => a.ID == id);
            
            if (albumToUpdate == null)
            {
                return NotFound();
            }

            _context.Entry(albumToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            if (await TryUpdateModelAsync<Album>(albumToUpdate, "", a => a.Name, a => a.Price, a => a.YearProduced))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    var exceptionEntry = ex.Entries.Single();
                    var clientValues = (Album)exceptionEntry.Entity;
                    var databaseEntry = exceptionEntry.GetDatabaseValues();
                    if (databaseEntry == null)
                    {
                        ModelState.AddModelError("",
                            "Unable to save changes. The Genre was deleted by another user.");
                    }
                    else
                    {
                        var databaseValues = (Album)databaseEntry.ToObject();
                        if (databaseValues.Name != clientValues.Name)
                            ModelState.AddModelError("Name", "Current value: "
                                + databaseValues.Name);
                        if (databaseValues.Price != clientValues.Price)
                            ModelState.AddModelError("Price", "Current value: "
                                + databaseValues.Price);
                        if (databaseValues.YearProduced != clientValues.YearProduced)
                            ModelState.AddModelError("YearProduced", "Current value: "
                                + databaseValues.YearProduced);

                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                                + "was modified by another user after you received your values. The "
                                + "edit operation was canceled and the current values in the database "
                                + "have been displayed. If you still want to save your version of this record, click "
                                + "the Save button again. Otherwise click the 'Back to Album List' hyperlink.");
                        albumToUpdate.RowVersion = (byte[])databaseValues.RowVersion;
                        ModelState.Remove("RowVersion");
                    };
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again.");
                }

            }
            return View(albumToUpdate);
        }

        // GET: Albums/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Albums == null)
            {
                return NotFound();
            }

            var album = await _context.Albums
                .Include(a => a.Genre)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (album == null)
            {
                return NotFound();
            }

            return View(album);
        }

        // POST: Albums/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Albums == null)
            {
                return Problem("Entity set 'MusicContext.Albums'  is null.");
            }
            var album = await _context.Albums.FindAsync(id);
            if (album != null)
            {
                _context.Albums.Remove(album);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AlbumExists(int id)
        {
          return _context.Albums.Any(e => e.ID == id);
        }
    }
}
