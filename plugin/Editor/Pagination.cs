using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace UnityMcp2021.Editor
{
    /// <summary>
    /// Represents a pagination request parsed from command parameters.
    /// Supports both cursor-based (0-based offset) and page_number-based (1-based) pagination modes.
    /// </summary>
    public class PaginationRequest
    {
        /// <summary>
        /// Maximum allowed page size. Values exceeding this are clamped.
        /// </summary>
        public const int MaxPageSize = 200;

        /// <summary>
        /// The number of items per page. Clamped to [1, 200].
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// The 0-based offset cursor indicating where to start reading items.
        /// </summary>
        public int Cursor { get; set; }

        /// <summary>
        /// Creates a PaginationRequest from a JObject containing command parameters.
        /// Supports both cursor (0-based offset) and page_number (1-based) modes.
        /// When page_number is provided instead of cursor: cursor = (page_number - 1) * page_size.
        /// </summary>
        /// <param name="parameters">The JObject containing pagination parameters. If null, defaults are used.</param>
        /// <param name="defaultPageSize">The default page size when not specified in parameters. Defaults to 50.</param>
        /// <returns>A configured PaginationRequest instance.</returns>
        public static PaginationRequest FromParams(JObject parameters, int defaultPageSize = 50)
        {
            ToolParams toolParams = new ToolParams(parameters);

            // Parse page_size (supports both "page_size" and "pageSize" via ToolParams normalization)
            int requestedPageSize = toolParams.GetInt("page_size", defaultPageSize);

            // Clamp page_size to [1, MaxPageSize]
            int pageSize = Math.Max(1, Math.Min(requestedPageSize, MaxPageSize));

            // Parse cursor (0-based offset)
            int cursor = toolParams.GetInt("cursor", -1);

            // If cursor is not provided, check for page_number (1-based)
            if (cursor < 0)
            {
                int pageNumber = toolParams.GetInt("page_number", -1);
                if (pageNumber > 0)
                {
                    // Convert 1-based page_number to 0-based cursor
                    cursor = (pageNumber - 1) * pageSize;
                }
                else
                {
                    // Default to start (cursor 0)
                    cursor = 0;
                }
            }

            // Ensure cursor is non-negative
            cursor = Math.Max(0, cursor);

            return new PaginationRequest
            {
                PageSize = pageSize,
                Cursor = cursor
            };
        }
    }

    /// <summary>
    /// Represents a paginated response containing a subset of items from a larger collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the paginated response.</typeparam>
    public class PaginationResponse<T>
    {
        /// <summary>
        /// The items in the current page.
        /// </summary>
        public List<T> Items { get; set; }

        /// <summary>
        /// The cursor (0-based offset) used for this page.
        /// </summary>
        public int Cursor { get; set; }

        /// <summary>
        /// The cursor for the next page, or null if there are no more pages.
        /// Equal to Cursor + Items.Count when HasMore is true.
        /// </summary>
        public int? NextCursor { get; set; }

        /// <summary>
        /// The total number of items in the full collection.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// The page size used for this response.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Whether there are more items beyond the current page.
        /// True when Cursor + Items.Count is less than TotalCount.
        /// </summary>
        public bool HasMore { get; set; }

        /// <summary>
        /// Creates a PaginationResponse by slicing a full list of items according to the pagination request.
        /// </summary>
        /// <param name="allItems">The complete list of items to paginate. If null, treated as empty.</param>
        /// <param name="request">The pagination request specifying cursor and page size.</param>
        /// <returns>A PaginationResponse containing the appropriate slice of items.</returns>
        public static PaginationResponse<T> Create(List<T> allItems, PaginationRequest request)
        {
            if (allItems == null)
            {
                allItems = new List<T>();
            }

            if (request == null)
            {
                request = new PaginationRequest { PageSize = 50, Cursor = 0 };
            }

            int totalCount = allItems.Count;
            int cursor = Math.Max(0, request.Cursor);
            int pageSize = Math.Max(1, Math.Min(request.PageSize, PaginationRequest.MaxPageSize));

            // Calculate how many items we can return from the cursor position
            int availableFromCursor = Math.Max(0, totalCount - cursor);
            int itemCount = Math.Min(pageSize, availableFromCursor);

            // Extract the page of items
            List<T> items;
            if (cursor >= totalCount)
            {
                items = new List<T>();
            }
            else
            {
                items = allItems.GetRange(cursor, itemCount);
            }

            // Determine if there are more items
            bool hasMore = cursor + items.Count < totalCount;

            // Calculate next cursor
            int? nextCursor = hasMore ? (int?)(cursor + items.Count) : null;

            return new PaginationResponse<T>
            {
                Items = items,
                Cursor = cursor,
                NextCursor = nextCursor,
                TotalCount = totalCount,
                PageSize = pageSize,
                HasMore = hasMore
            };
        }

        /// <summary>
        /// Converts this pagination response to a JObject for JSON serialization.
        /// </summary>
        /// <param name="itemSerializer">A function to convert each item to a JToken. If null, items are serialized using JToken.FromObject.</param>
        /// <returns>A JObject representing the paginated response.</returns>
        public JObject ToJObject(Func<T, JToken> itemSerializer = null)
        {
            JArray itemsArray = new JArray();
            foreach (T item in Items)
            {
                if (itemSerializer != null)
                {
                    itemsArray.Add(itemSerializer(item));
                }
                else
                {
                    itemsArray.Add(JToken.FromObject(item));
                }
            }

            JObject result = new JObject
            {
                ["items"] = itemsArray,
                ["cursor"] = Cursor,
                ["nextCursor"] = NextCursor.HasValue ? (JToken)NextCursor.Value : JValue.CreateNull(),
                ["totalCount"] = TotalCount,
                ["pageSize"] = PageSize,
                ["hasMore"] = HasMore
            };

            return result;
        }
    }
}
