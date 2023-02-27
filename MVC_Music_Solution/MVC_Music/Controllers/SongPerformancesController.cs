using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVC_Music.Data;
using MVC_Music.Models;
using MVC_Music.Utilities;

namespace MVC_Music.Controllers
{
    public class SongPerformancesController : CustomControllers.CognizantController
    {
        private readonly MusicContext _context;

        public SongPerformancesController(MusicContext context)
        {
            _context = context;
        }

        // GET: SongPerformances
        public async Task<IActionResult> Index(int? songID, int? page, int? pageSizeID, int? musicianID, int? instrumentID, string actionButton,
            string SearchString, string sortDirection = "desc", string sortField = "Musician")
        {

            //CookieHelper.CookieSet(HttpContext, ControllerName() + "URL", "", -1);

            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Song Performances");

            if (!songID.HasValue)
            {
                return Redirect(ViewData["returnURL"].ToString());
            }
            PopulateDropDownLists();

            ViewData["Filtering"] = "btn-outline-dark"; 

            string[] sortOptions = new[] { "Musician", "Instrument", "Fee Paid" };

            var performances = from p in _context.Performances
                        .Include(p => p.Musician)
                        .Include(p => p.Instrument)
                        where p.SongID == songID.GetValueOrDefault()
                        select p;

            if (musicianID.HasValue)
            {
                performances = performances.Where(p => p.ID == musicianID);
                ViewData["Filtering"] = "btn-danger";
            }
            if (instrumentID.HasValue)
            {
                performances = performances.Where(p => p.ID == instrumentID);
                ViewData["Filtering"] = "btn-danger";
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                performances = performances.Where(p => p.Comments.ToUpper().Contains(SearchString.ToUpper()));
                ViewData["Filtering"] = "btn-danger";
            }

            if (!String.IsNullOrEmpty(actionButton)) 
            {
                page = 1;

                if (sortOptions.Contains(actionButton))
                {
                    if (actionButton == sortField)
                    {
                        sortDirection = sortDirection == "asc" ? "desc" : "asc";
                    }
                    sortField = actionButton;
                }
            }

            if (sortField == "Musician")
            {
                if (sortDirection == "asc")
                {
                    performances = performances
                        .OrderBy(p => p.Musician.LastName);
                }
                else
                {
                    performances = performances
                        .OrderByDescending(p => p.Musician.LastName);
                }
            }
            else if (sortField == "Instrument")
            {
                if (sortDirection == "asc")
                {
                    performances = performances
                        .OrderBy(p => p.Instrument);
                }
                else
                {
                    performances = performances
                        .OrderByDescending(p => p.Instrument);
                }
            }
            else // Fees Paid
            {
                if (sortDirection == "asc")
                {
                    performances = performances
                        .OrderBy(p => p.FeePaid);
                }
                else
                {
                    performances = performances
                        .OrderByDescending(p => p.FeePaid);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;


            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, ControllerName());
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<Performance>.CreateAsync(performances.AsNoTracking(), page ?? 1, pageSize);

            //getting master record

            Song song = _context.Songs
                //.Include(s => s.DateRecorded)
                .Include(s => s.Album)
                .Include(s => s.Genre)
                .Where(s => s.ID == songID.GetValueOrDefault())
                .AsNoTracking()
                .FirstOrDefault();

            ViewBag.Song = song;

            return View(pagedData);
        }
       
        // GET: SongPerformances/Add
        public IActionResult Add(int? SongID, string SongTitle)
        {
            ViewDataReturnURL();

            if (!SongID.HasValue)
            {
                return Redirect(ViewData["returnURL"].ToString());
            }
            ViewData["SongTitle"] = SongTitle;

            Performance p = new Performance()
            {
                SongID = SongID.GetValueOrDefault()
            };
            PopulateDropDownLists();
            return View(p);
        }

        // POST: SongPerformances/Add
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([Bind("ID,Comments,FeePaid,SongID,MusicianID," +
            "InstrumentID")] Performance performance, string SongTitle)
        {

            ViewDataReturnURL();
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(performance);
                    await _context.SaveChangesAsync();
                    return Redirect(ViewData["returnURL"].ToString());
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes.  Talk to your system administrator if it continues.");
            }

            PopulateDropDownLists(performance);
            ViewData["SongTitle"] = SongTitle;
            ViewData["InstrumentID"] = new SelectList(_context.Instruments, "ID", "Name", performance.InstrumentID);
            ViewData["MusicianID"] = new SelectList(_context.Musicians, "ID", "FullName", performance.MusicianID);
            ViewData["SongID"] = new SelectList(_context.Songs, "ID", "Title", performance.SongID);
            return View(performance);
        }
        // GET: PatientAppt/Update/5
        public async Task<IActionResult> Update(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            //Get the URL with the last filter, sort and page parameters
            ViewDataReturnURL();

            var performance = await _context.Performances
                                .Include(p => p.Song)
                                .Include(p => p.Musician)
                                .Include(p => p.Instrument)
                                .AsNoTracking()
                                .FirstOrDefaultAsync(m => m.ID == id);

            if (performance == null)
            {
                return NotFound();
            }
            PopulateDropDownLists(performance);
            return View(performance);
        }

        // POST: PatientAppt/Update/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id)
        {
            //Get the URL with the last filter, sort and page parameters
            ViewDataReturnURL();

            var performanceToUpdate = await _context.Performances
                .Include(p => p.Musician)
                .Include(p => p.Instrument)
                .Include(p => p.Song)
                .FirstOrDefaultAsync(m => m.ID == id);

            //Check that you got it or exit with a not found error
            if (performanceToUpdate == null)
            {
                return NotFound();
            }

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Performance>(performanceToUpdate, "",
                p => p.MusicianID, p => p.InstrumentID, p => p.FeePaid, p => p.Comments))
            {
                try
                {
                    _context.Update(performanceToUpdate);
                    await _context.SaveChangesAsync();
                    return Redirect(ViewData["returnURL"].ToString());
                }

                catch (DbUpdateConcurrencyException)
                {
                    if (!PerformanceExists(performanceToUpdate.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem " +
                        "persists see your system administrator.");
                }
            }
            PopulateDropDownLists(performanceToUpdate);
            return View(performanceToUpdate);
        }

        // GET: PatientAppt/Remove/5
        public async Task<IActionResult> Remove(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Get the URL with the last filter, sort and page parameters
            ViewDataReturnURL();

            var performance = await _context.Performances
                .Include(p => p.Musician)
                .Include(p => p.Song)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (performance == null)
            {
                return NotFound();
            }
            return View(performance);
        }

        // POST: PatientAppt/Remove/5
        [HttpPost, ActionName("Remove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveConfirmed(int id)
        {
            var performance = await _context.Performances
                .Include(p => p.Musician)
                .Include(p => p.Song)
                .FirstOrDefaultAsync(m => m.ID == id);

            //Get the URL with the last filter, sort and page parameters
            ViewDataReturnURL();

            try
            {
                _context.Performances.Remove(performance);
                await _context.SaveChangesAsync();
                return Redirect(ViewData["returnURL"].ToString());
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem " +
                    "persists see your system administrator.");
            }

            return View(performance);
        }
    
    //// GET: SongPerformances/Edit/5
    //public async Task<IActionResult> Edit(int? id)
    //{
    //    if (id == null || _context.Performances == null)
    //    {
    //        return NotFound();
    //    }

    //    var performance = await _context.Performances.FindAsync(id);
    //    if (performance == null)
    //    {
    //        return NotFound();
    //    }
    //    ViewData["InstrumentID"] = new SelectList(_context.Instruments, "ID", "Name", performance.InstrumentID);
    //    ViewData["MusicianID"] = new SelectList(_context.Musicians, "ID", "FirstName", performance.MusicianID);
    //    ViewData["SongID"] = new SelectList(_context.Songs, "ID", "Title", performance.SongID);
    //    return View(performance);
    //}

    //// POST: SongPerformances/Edit/5
    //// To protect from overposting attacks, enable the specific properties you want to bind to.
    //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    //[HttpPost]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> Edit(int id, [Bind("ID,Comments,FeePaid,SongID,MusicianID,InstrumentID")] Performance performance)
    //{
    //    if (id != performance.MusicianID)
    //    {
    //        return NotFound();
    //    }

    //    if (ModelState.IsValid)
    //    {
    //        try
    //        {
    //            _context.Update(performance);
    //            await _context.SaveChangesAsync();
    //        }
    //        catch (DbUpdateConcurrencyException)
    //        {
    //            if (!PerformanceExists(performance.MusicianID))
    //            {
    //                return NotFound();
    //            }
    //            else
    //            {
    //                throw;
    //            }
    //        }
    //        return RedirectToAction(nameof(Index));
    //    }
    //    ViewData["InstrumentID"] = new SelectList(_context.Instruments, "ID", "Name", performance.InstrumentID);
    //    ViewData["MusicianID"] = new SelectList(_context.Musicians, "ID", "FirstName", performance.MusicianID);
    //    ViewData["SongID"] = new SelectList(_context.Songs, "ID", "Title", performance.SongID);
    //    return View(performance);
    //}

    //// GET: SongPerformances/Delete/5
    //public async Task<IActionResult> Delete(int? id)
    //{
    //    if (id == null || _context.Performances == null)
    //    {
    //        return NotFound();
    //    }

    //    var performance = await _context.Performances
    //        .Include(p => p.Instrument)
    //        .Include(p => p.Musician)
    //        .Include(p => p.Song)
    //        .FirstOrDefaultAsync(m => m.MusicianID == id);
    //    if (performance == null)
    //    {
    //        return NotFound();
    //    }

    //    return View(performance);
    //}

    //// POST: SongPerformances/Delete/5
    //[HttpPost, ActionName("Delete")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> DeleteConfirmed(int id)
    //{
    //    if (_context.Performances == null)
    //    {
    //        return Problem("Entity set 'MusicContext.Performances'  is null.");
    //    }
    //    var performance = await _context.Performances.FindAsync(id);
    //    if (performance != null)
    //    {
    //        _context.Performances.Remove(performance);
    //    }

    //    await _context.SaveChangesAsync();
    //    return RedirectToAction(nameof(Index));
    //}

        private bool PerformanceExists(int id)
        {
            return _context.Performances.Any(e => e.MusicianID == id);
        }
        private SelectList MusicianSelectList(int? id)
        {
            var mQuery = from m in _context.Musicians
                         orderby m.LastName
                         select m;
            return new SelectList(mQuery, "ID", "FullName", id);
        }
        private SelectList InstrumentSelectList(int? id)
        {
            var iQuery = from i in _context.Instruments
                         orderby i.Name
                         select i;
            return new SelectList(iQuery, "ID", "Name", id);
        }
        private SelectList PerformanceSelectList(int? id)
        {
            var iQuery = from i in _context.Performances
                         orderby i.ID
                         select i;
            return new SelectList(iQuery, "ID", "Name", id);
        }
        private void PopulateDropDownLists(Performance performance = null)
        {
            ViewData["MusicianID"] = MusicianSelectList(performance?.MusicianID);
            ViewData["InstrumentID"] = InstrumentSelectList(performance?.InstrumentID);
        }
        private void ViewDataReturnURL()
        {
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, ControllerName());
        }
    }
}
